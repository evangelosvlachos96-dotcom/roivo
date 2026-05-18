namespace Roivo.Application.Features.Aade.Commands.DisconnectAade;

public abstract record DisconnectAadeResult
{
    public sealed record Success : DisconnectAadeResult;
    public sealed record Forbidden(string Reason) : DisconnectAadeResult;
    public sealed record BusinessNotFound : DisconnectAadeResult;
}
