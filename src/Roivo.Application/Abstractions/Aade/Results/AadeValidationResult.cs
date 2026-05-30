namespace Roivo.Application.Abstractions.Aade.Results;

/// <summary>
/// Outcome of an AADE credential-validation call. AADE's RequestDocs endpoint
/// validates credentials AND AFM authorization in a single round-trip, so every
/// failure mode is distinct and synchronous.
/// </summary>
public abstract record AadeValidationResult
{
    public sealed record Success : AadeValidationResult;
    public sealed record InvalidCredentials : AadeValidationResult;

    /// <summary>
    /// Credentials authenticate but are not authorized for the supplied AFM.
    /// <c>CredentialsAfm</c> is the AFM AADE associates with the credentials,
    /// parsed from the error body when possible; <c>null</c> if extraction fails.
    /// </summary>
    public sealed record AfmMismatch(string? CredentialsAfm) : AadeValidationResult;

    public sealed record RateLimited : AadeValidationResult;
    public sealed record AadeServerError(int StatusCode, string Message) : AadeValidationResult;
    public sealed record NetworkError(string Message) : AadeValidationResult;
}
