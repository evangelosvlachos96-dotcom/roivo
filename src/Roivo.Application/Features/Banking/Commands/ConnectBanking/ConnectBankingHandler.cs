using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Banking;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Banking.Commands.ConnectBanking;

public sealed class ConnectBankingHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IBankingClient _banking;
    private readonly IBankingCredentialStore _credentials;
    private readonly IBankingConnectionOptions _options;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public ConnectBankingHandler(
        IBusinessRepository businesses,
        IBankingClient banking,
        IBankingCredentialStore credentials,
        IBankingConnectionOptions options,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(banking);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _businesses = businesses;
        _banking = banking;
        _credentials = credentials;
        _options = options;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<ConnectBankingResult> Handle(ConnectBankingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanManageBankingFor(_tenant.CurrentTenantType))
            return new ConnectBankingResult.Forbidden("Δεν επιτρέπεται η διαχείριση τραπεζικής σύνδεσης από αυτόν τον τύπο λογαριασμού.");

        if (string.IsNullOrWhiteSpace(command.BankName))
            return new ConnectBankingResult.UnknownProvider(command.BankName ?? string.Empty);

        var business = await _businesses.GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return new ConnectBankingResult.BusinessNotFound();

        if (await _credentials.HasCredentialsAsync(business.Id, cancellationToken).ConfigureAwait(false))
            return new ConnectBankingResult.AlreadyConnected();

        // The business id travels as `state` so the callback knows which business
        // it belongs to. It is not a secret and is not trusted: the completion
        // handler re-checks permissions and tenant ownership, and the
        // authorization code is what actually proves consent.
        var request = new BankingAuthorizationRequest(
            BankName: command.BankName,
            Country: _options.Country,
            RedirectUrl: _options.RedirectUrl,
            State: business.Id.ToString("N"),
            AccessValidUntil: DateTimeOffset.UtcNow.AddDays(_options.ConsentValidDays),
            Language: "el");

        var authorization = await _banking.StartAuthorizationAsync(request, cancellationToken).ConfigureAwait(false);

        switch (authorization)
        {
            case BankingAuthorizationResult.UnknownProvider:
                return new ConnectBankingResult.UnknownProvider(command.BankName);
            case BankingAuthorizationResult.Unauthorized u:
                return new ConnectBankingResult.BankingUnavailable(u.Message);
            case BankingAuthorizationResult.NetworkError ne:
                return new ConnectBankingResult.BankingUnavailable(ne.Message);
            case BankingAuthorizationResult.BankingServerError se:
                return new ConnectBankingResult.BankingUnavailable($"Banking API returned {se.StatusCode}: {se.Message}");
        }

        var success = (BankingAuthorizationResult.Success)authorization;

        // Logged at initiation, not only on completion, so an abandoned consent
        // still leaves a trace of who tried to connect what.
        await _audit.WriteAsync(
            action: AuditAction.BankingConnectionInitiated,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, command.BankName, success.AuthorizationId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ConnectBankingResult.Success(success.AuthorizationUrl);
    }
}
