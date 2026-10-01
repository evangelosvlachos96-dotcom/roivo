using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Banking;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Banking.Commands.CompleteBankingConnection;

public sealed class CompleteBankingConnectionHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IBankAccountRepository _accounts;
    private readonly IBankingClient _banking;
    private readonly IBankingCredentialStore _credentials;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public CompleteBankingConnectionHandler(
        IBusinessRepository businesses,
        IBankAccountRepository accounts,
        IBankingClient banking,
        IBankingCredentialStore credentials,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(banking);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _businesses = businesses;
        _accounts = accounts;
        _banking = banking;
        _credentials = credentials;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<CompleteBankingConnectionResult> Handle(
        CompleteBankingConnectionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanManageBankingFor(_tenant.CurrentTenantType))
            return new CompleteBankingConnectionResult.Forbidden("Δεν επιτρέπεται η διαχείριση τραπεζικής σύνδεσης από αυτόν τον τύπο λογαριασμού.");

        if (string.IsNullOrWhiteSpace(command.AuthorizationCode))
            return new CompleteBankingConnectionResult.InvalidCode();

        // GetByIdActiveOnlyAsync applies the tenant query filter, so a business
        // id smuggled in through the redirect's `state` resolves to null unless
        // it really belongs to the signed-in tenant.
        var business = await _businesses.GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return new CompleteBankingConnectionResult.BusinessNotFound();

        var session = await _banking.CompleteAuthorizationAsync(command.AuthorizationCode, cancellationToken).ConfigureAwait(false);

        switch (session)
        {
            case BankingSessionResult.InvalidCode:
            case BankingSessionResult.SessionExpired:
                return new CompleteBankingConnectionResult.InvalidCode();
            case BankingSessionResult.Unauthorized u:
                return new CompleteBankingConnectionResult.BankingUnavailable(u.Message);
            case BankingSessionResult.NetworkError ne:
                return new CompleteBankingConnectionResult.BankingUnavailable(ne.Message);
            case BankingSessionResult.BankingServerError se:
                return new CompleteBankingConnectionResult.BankingUnavailable($"Banking API returned {se.StatusCode}: {se.Message}");
        }

        var success = (BankingSessionResult.Success)session;

        // A session with no accounts would look connected in the UI but never
        // produce a transaction. Refuse it rather than store a dead connection.
        if (success.Accounts.Count == 0)
            return new CompleteBankingConnectionResult.NoAccountsGranted();

        await _credentials.StoreAsync(business.Id, success.SessionId, cancellationToken).ConfigureAwait(false);

        foreach (var account in success.Accounts)
        {
            await _accounts.UpsertAsync(new BankAccount
            {
                TenantId = business.TenantId,
                BusinessId = business.Id,
                ExternalAccountUid = account.Uid,
                BankName = string.IsNullOrWhiteSpace(success.BankName) ? "—" : success.BankName,
                Iban = account.Iban ?? account.Uid,
                Currency = ParseCurrency(account.Currency),
            }, cancellationToken).ConfigureAwait(false);
        }

        business.RecordBankingConnection(
            providerName: string.IsNullOrWhiteSpace(success.BankName) ? "—" : success.BankName,
            consentExpiresAtUtc: success.AccessValidUntil?.UtcDateTime);
        await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

        // Audit records metadata only — never the session id.
        await _audit.WriteAsync(
            action: AuditAction.BankingConnectionConfirmed,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, success.BankName, AccountCount = success.Accounts.Count },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new CompleteBankingConnectionResult.Success(success.BankName, success.Accounts.Count);
    }

    private static Currency ParseCurrency(string code)
        => Enum.TryParse<Currency>(code, ignoreCase: true, out var parsed) ? parsed : Currency.EUR;
}
