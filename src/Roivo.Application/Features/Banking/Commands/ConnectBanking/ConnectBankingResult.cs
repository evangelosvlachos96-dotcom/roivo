namespace Roivo.Application.Features.Banking.Commands.ConnectBanking;

public abstract record ConnectBankingResult
{
    /// <summary>
    /// <c>AuthorizationUrl</c> is the bank's consent page — the caller must send
    /// the user there. Nothing is persisted until they come back with a code.
    /// </summary>
    public sealed record Success(string AuthorizationUrl) : ConnectBankingResult;

    public sealed record BusinessNotFound : ConnectBankingResult;
    public sealed record Forbidden(string Reason) : ConnectBankingResult;

    /// <summary>The business already has a live connection; disconnect first.</summary>
    public sealed record AlreadyConnected : ConnectBankingResult;

    public sealed record UnknownProvider(string BankName) : ConnectBankingResult;
    public sealed record BankingUnavailable(string Message) : ConnectBankingResult;
}
