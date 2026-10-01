namespace Roivo.Application.Features.Banking.Commands.SyncBusinessBankTransactions;

public abstract record SyncBusinessBankTransactionsResult
{
    public sealed record Success(int NewCount, int UpdatedCount, int AccountCount) : SyncBusinessBankTransactionsResult;

    public sealed record BusinessNotFound : SyncBusinessBankTransactionsResult;
    public sealed record NotConnected : SyncBusinessBankTransactionsResult;

    /// <summary>The bank-side consent lapsed or was revoked. Needs the user to
    /// re-consent — retrying achieves nothing.</summary>
    public sealed record SessionExpired : SyncBusinessBankTransactionsResult;

    public sealed record NetworkError(string Message) : SyncBusinessBankTransactionsResult;
    public sealed record BankingServerError(int StatusCode, string Message) : SyncBusinessBankTransactionsResult;
}
