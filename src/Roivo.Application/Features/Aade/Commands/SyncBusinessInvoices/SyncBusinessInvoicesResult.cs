namespace Roivo.Application.Features.Aade.Commands.SyncBusinessInvoices;

public abstract record SyncBusinessInvoicesResult
{
    public sealed record Success(int NewCount, int UpdatedCount) : SyncBusinessInvoicesResult;
    public sealed record BusinessNotFound : SyncBusinessInvoicesResult;
    public sealed record NotConnected : SyncBusinessInvoicesResult;
    public sealed record InvalidCredentials : SyncBusinessInvoicesResult;
    public sealed record NetworkError(string Message) : SyncBusinessInvoicesResult;
    public sealed record AadeServerError(int StatusCode, string Message) : SyncBusinessInvoicesResult;
}
