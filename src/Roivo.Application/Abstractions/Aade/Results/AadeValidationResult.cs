namespace Roivo.Application.Abstractions.Aade.Results;

/// <summary>Outcome of an AADE credential-validation call.</summary>
public abstract record AadeValidationResult
{
    /// <summary>Credentials are valid; AADE returned the AFM they belong to.</summary>
    public sealed record Success(string AfmFromAade) : AadeValidationResult;
    public sealed record InvalidCredentials : AadeValidationResult;
    public sealed record NetworkError(string Message) : AadeValidationResult;
    public sealed record AadeServerError(int StatusCode, string Message) : AadeValidationResult;
}
