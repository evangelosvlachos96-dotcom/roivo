using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Aade;

namespace Roivo.Application.Features.Aade.Queries.GetAadeConnectionStatus;

public sealed class GetAadeConnectionStatusHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IAadeCredentialStore _credentials;

    public GetAadeConnectionStatusHandler(
        IBusinessRepository businesses,
        IAadeCredentialStore credentials)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(credentials);

        _businesses = businesses;
        _credentials = credentials;
    }

    public async Task<AadeConnectionStatus> Handle(GetAadeConnectionStatusQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var business = await _businesses.GetByIdActiveOnlyAsync(query.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return new AadeConnectionStatus(IsConnected: false, LastSyncAt: null, MaskedUserId: null);

        var creds = await _credentials.RetrieveAsync(business.Id, cancellationToken).ConfigureAwait(false);
        if (creds is null)
            return new AadeConnectionStatus(IsConnected: false, LastSyncAt: business.LastAadeSyncAt, MaskedUserId: null);

        return new AadeConnectionStatus(
            IsConnected: true,
            LastSyncAt: business.LastAadeSyncAt,
            MaskedUserId: Mask(creds.UserId));
    }

    private static string Mask(string userId)
        => userId.Length <= 4 ? new string('*', userId.Length) : userId[..4] + "***";
}
