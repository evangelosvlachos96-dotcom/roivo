namespace Roivo.Core.Domain.Exceptions;

public sealed class InvalidAfmException : DomainException
{
    public string AttemptedAfm { get; }

    public InvalidAfmException(string attemptedAfm)
        : base($"Invalid AFM: {attemptedAfm}")
    {
        AttemptedAfm = attemptedAfm;
    }
}
