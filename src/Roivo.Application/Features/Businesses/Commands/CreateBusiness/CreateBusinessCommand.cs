namespace Roivo.Application.Features.Businesses.Commands.CreateBusiness;

/// <summary>Input for creating a new business in the current tenant.</summary>
public sealed record CreateBusinessCommand(
    string Name,
    string Afm,
    string? Kad,
    string? Address);
