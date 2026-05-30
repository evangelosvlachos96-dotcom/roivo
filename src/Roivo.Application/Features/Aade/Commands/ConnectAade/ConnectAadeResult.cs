namespace Roivo.Application.Features.Aade.Commands.ConnectAade;

public abstract record ConnectAadeResult
{
    public sealed record Success(Guid BusinessId) : ConnectAadeResult;
    public sealed record BusinessNotFound : ConnectAadeResult;
    public sealed record Forbidden(string Reason) : ConnectAadeResult;
    public sealed record InvalidCredentials : ConnectAadeResult;
    /// <summary>
    /// Credentials are valid but belong to a different AFM than the business holds.
    /// <c>CredentialsAfm</c> is the AFM AADE associates with the credentials
    /// (parsed from the error body); <c>null</c> if extraction failed.
    /// </summary>
    public sealed record AfmMismatch(string BusinessAfm, string? CredentialsAfm) : ConnectAadeResult;
    public sealed record RateLimited : ConnectAadeResult;
    public sealed record AadeUnavailable(string Message) : ConnectAadeResult;
}
