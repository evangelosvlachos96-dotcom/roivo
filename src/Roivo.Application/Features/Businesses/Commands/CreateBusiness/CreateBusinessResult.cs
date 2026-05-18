namespace Roivo.Application.Features.Businesses.Commands.CreateBusiness;

/// <summary>Outcomes of <see cref="CreateBusinessHandler"/>.</summary>
public abstract record CreateBusinessResult
{
    public sealed record Success(Guid BusinessId) : CreateBusinessResult;
    public sealed record Forbidden(string Reason) : CreateBusinessResult;
    public sealed record InvalidAfm : CreateBusinessResult;
    public sealed record ActiveDuplicate(string Afm) : CreateBusinessResult;
    public sealed record InactiveDuplicate(Guid ExistingId, string OldName, string? OldAddress) : CreateBusinessResult;
}
