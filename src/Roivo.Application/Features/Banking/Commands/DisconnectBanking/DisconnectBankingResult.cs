namespace Roivo.Application.Features.Banking.Commands.DisconnectBanking;

public abstract record DisconnectBankingResult
{
    public sealed record Success : DisconnectBankingResult;
    public sealed record Forbidden(string Reason) : DisconnectBankingResult;
    public sealed record BusinessNotFound : DisconnectBankingResult;
}
