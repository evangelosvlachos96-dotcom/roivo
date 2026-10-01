namespace Roivo.Application.Features.Banking.Commands.CompleteBankingConnection;

public abstract record CompleteBankingConnectionResult
{
    public sealed record Success(string BankName, int AccountCount) : CompleteBankingConnectionResult;

    public sealed record BusinessNotFound : CompleteBankingConnectionResult;
    public sealed record Forbidden(string Reason) : CompleteBankingConnectionResult;

    /// <summary>The code was already used, expired, or never issued to us. The
    /// user recovers by restarting the consent flow.</summary>
    public sealed record InvalidCode : CompleteBankingConnectionResult;

    /// <summary>Consent succeeded but granted access to no accounts, so there is
    /// nothing to sync — storing the session would look connected but never
    /// produce data.</summary>
    public sealed record NoAccountsGranted : CompleteBankingConnectionResult;

    public sealed record BankingUnavailable(string Message) : CompleteBankingConnectionResult;
}
