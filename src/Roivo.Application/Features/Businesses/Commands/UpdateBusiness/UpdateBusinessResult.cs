namespace Roivo.Application.Features.Businesses.Commands.UpdateBusiness;

/// <summary>Outcomes of <see cref="UpdateBusinessHandler"/>.</summary>
public abstract record UpdateBusinessResult
{
    public sealed record Success : UpdateBusinessResult;
    public sealed record Forbidden(string Reason) : UpdateBusinessResult;
    public sealed record InvalidAfm : UpdateBusinessResult;
    public sealed record NotFound : UpdateBusinessResult;
    public sealed record ActiveDuplicateAfm(string Afm) : UpdateBusinessResult;
}
