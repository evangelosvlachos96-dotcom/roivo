using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Banking;

namespace Roivo.Application.Features.Banking.Queries.GetBankingConnectionStatus;

public sealed class GetBankingConnectionStatusHandler
{
    /// <summary>
    /// Failure streak at which the UI stops showing "connected, last synced X"
    /// and starts prompting a reconnect. Three nights of failure is past any
    /// plausible transient outage.
    /// </summary>
    public const int ErrorStateThreshold = 3;

    private readonly IBusinessRepository _businesses;
    private readonly IBankAccountRepository _accounts;
    private readonly IBankingCredentialStore _credentials;

    public GetBankingConnectionStatusHandler(
        IBusinessRepository businesses,
        IBankAccountRepository accounts,
        IBankingCredentialStore credentials)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(credentials);

        _businesses = businesses;
        _accounts = accounts;
        _credentials = credentials;
    }

    public async Task<BankingConnectionInfo> Handle(
        GetBankingConnectionStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var business = await _businesses.GetByIdActiveOnlyAsync(query.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return Disconnected();

        if (!await _credentials.HasCredentialsAsync(business.Id, cancellationToken).ConfigureAwait(false))
            return Disconnected();

        var accounts = await _accounts.ListByBusinessAsync(business.Id, cancellationToken).ConfigureAwait(false);

        var state = business.BankingSyncErrorCount >= ErrorStateThreshold
            ? BankingConnectionState.Error
            : BankingConnectionState.Connected;

        return new BankingConnectionInfo(
            State: state,
            BankName: business.BankingProviderName,
            LastSyncAt: business.LastBankingSyncAt,
            ConsentExpiresAt: business.BankingConsentExpiresAt,
            ErrorCount: business.BankingSyncErrorCount,
            LastFailureReason: business.BankingLastFailureReason,
            Accounts: accounts
                .Select(a => new BankingAccountSummary(a.Id, a.BankName, a.Iban, a.Currency.ToString()))
                .ToList());
    }

    private static BankingConnectionInfo Disconnected()
        => new(BankingConnectionState.Disconnected, null, null, null, 0, null, []);
}
