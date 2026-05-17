namespace Roivo.Application.Features.Businesses.Commands.ReactivateBusiness;

public abstract record ReactivateBusinessResult
{
    public sealed record Success : ReactivateBusinessResult;
    public sealed record NotFound : ReactivateBusinessResult;
    public sealed record AlreadyActive : ReactivateBusinessResult;
    public sealed record ConflictsWithActive(string Afm) : ReactivateBusinessResult;
}
