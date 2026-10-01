using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Banking;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Banking.Commands.SyncBusinessBankTransactions;

public sealed class SyncBusinessBankTransactionsHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IBankAccountRepository _accounts;
    private readonly IBankTransactionRepository _transactions;
    private readonly IBankingClient _banking;
    private readonly IBankingCredentialStore _credentials;
    private readonly IBankingConnectionOptions _options;
    private readonly IAuditWriter _audit;

    public SyncBusinessBankTransactionsHandler(
        IBusinessRepository businesses,
        IBankAccountRepository accounts,
        IBankTransactionRepository transactions,
        IBankingClient banking,
        IBankingCredentialStore credentials,
        IBankingConnectionOptions options,
        IAuditWriter audit)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(banking);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(audit);

        _businesses = businesses;
        _accounts = accounts;
        _transactions = transactions;
        _banking = banking;
        _credentials = credentials;
        _options = options;
        _audit = audit;
    }

    /// <summary>
    /// Pulls the configured look-back window for every account of the business
    /// and upserts it. No permission check: this runs from the nightly cron as
    /// well as from the UI, and the UI has already authorised the user by the
    /// time it gets here. Tenant isolation still applies unless the caller sets
    /// <see cref="SyncBusinessBankTransactionsCommand.BypassTenantScope"/>.
    /// </summary>
    public async Task<SyncBusinessBankTransactionsResult> Handle(
        SyncBusinessBankTransactionsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var business = command.BypassTenantScope
            ? await _businesses.GetByIdActiveOnlyAcrossAllTenantsAsync(command.BusinessId, cancellationToken).ConfigureAwait(false)
            : await _businesses.GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken).ConfigureAwait(false);

        if (business is null)
            return new SyncBusinessBankTransactionsResult.BusinessNotFound();

        var sessionId = await _credentials.RetrieveAsync(business.Id, cancellationToken).ConfigureAwait(false);
        if (sessionId is null)
            return new SyncBusinessBankTransactionsResult.NotConnected();

        var bankAccounts = await _accounts.ListByBusinessAcrossAllTenantsAsync(business.Id, cancellationToken).ConfigureAwait(false);
        var syncable = bankAccounts.Where(a => !string.IsNullOrEmpty(a.ExternalAccountUid)).ToList();
        if (syncable.Count == 0)
            return new SyncBusinessBankTransactionsResult.NotConnected();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-_options.SyncLookbackDays);

        var fetched = new List<BankTransaction>();

        foreach (var account in syncable)
        {
            var fetch = await _banking
                .FetchTransactionsAsync(account.ExternalAccountUid!, from, today, cancellationToken)
                .ConfigureAwait(false);

            // One account failing fails the whole sync: a partial result would
            // stamp LastBankingSyncAt and hide the gap until someone noticed
            // missing transactions months later.
            switch (fetch)
            {
                case BankingFetchResult.SessionExpired:
                    return await RecordFailureAsync(business, "SessionExpired", cancellationToken,
                        new SyncBusinessBankTransactionsResult.SessionExpired()).ConfigureAwait(false);
                case BankingFetchResult.Unauthorized u:
                    return await RecordFailureAsync(business, "Unauthorized", cancellationToken,
                        new SyncBusinessBankTransactionsResult.BankingServerError(401, u.Message)).ConfigureAwait(false);
                case BankingFetchResult.NetworkError ne:
                    return await RecordFailureAsync(business, "NetworkError", cancellationToken,
                        new SyncBusinessBankTransactionsResult.NetworkError(ne.Message)).ConfigureAwait(false);
                case BankingFetchResult.BankingServerError se:
                    return await RecordFailureAsync(business, "BankingServerError", cancellationToken,
                        new SyncBusinessBankTransactionsResult.BankingServerError(se.StatusCode, se.Message)).ConfigureAwait(false);
            }

            var success = (BankingFetchResult.Success)fetch;
            foreach (var dto in success.Transactions)
            {
                fetched.Add(new BankTransaction
                {
                    // TenantId is set explicitly: the sync runs in a background
                    // scope with no ambient tenant, so the DbContext's
                    // SaveChanges hook would leave it empty.
                    TenantId = account.TenantId,
                    BankAccountId = account.Id,
                    ExternalId = dto.ExternalId,
                    BookingDate = dto.BookingDate,
                    ValueDate = dto.ValueDate,
                    Amount = dto.Amount,
                    Currency = ParseCurrency(dto.Currency),
                    CounterpartyName = dto.CounterpartyName,
                    CounterpartyIban = dto.CounterpartyIban,
                    Reference = dto.Reference,
                    RawPayload = dto.RawPayload,
                });
            }
        }

        var counts = await _transactions.UpsertRangeAsync(fetched, cancellationToken).ConfigureAwait(false);

        business.RecordBankingSyncSuccess(DateTime.UtcNow);
        await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            action: AuditAction.BankingSyncCompleted,
            tenantId: business.TenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, counts.Inserted, counts.Updated, AccountCount = syncable.Count },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new SyncBusinessBankTransactionsResult.Success(counts.Inserted, counts.Updated, syncable.Count);
    }

    private async Task<SyncBusinessBankTransactionsResult> RecordFailureAsync(
        Business business,
        string reason,
        CancellationToken cancellationToken,
        SyncBusinessBankTransactionsResult result)
    {
        business.RecordBankingSyncFailure(reason);
        await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            action: AuditAction.BankingSyncFailed,
            tenantId: business.TenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, Reason = reason, business.BankingSyncErrorCount },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }

    private static Currency ParseCurrency(string code)
        => Enum.TryParse<Currency>(code, ignoreCase: true, out var parsed) ? parsed : Currency.EUR;
}
