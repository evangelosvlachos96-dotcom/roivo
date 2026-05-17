namespace Roivo.Application.Features.Businesses.Commands.UpdateBusiness;

/// <summary>Input for updating an existing active business.</summary>
public sealed record UpdateBusinessCommand(
    Guid Id,
    string Name,
    string Afm,
    string? Kad,
    string? Address);
