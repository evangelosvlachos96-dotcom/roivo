namespace Roivo.Application.Features.Businesses.Commands.DeactivateBusiness;

public abstract record DeactivateBusinessResult
{
    public sealed record Success : DeactivateBusinessResult;
    public sealed record Forbidden(string Reason) : DeactivateBusinessResult;
    public sealed record NotFound : DeactivateBusinessResult;
    public sealed record AlreadyInactive : DeactivateBusinessResult;
}
