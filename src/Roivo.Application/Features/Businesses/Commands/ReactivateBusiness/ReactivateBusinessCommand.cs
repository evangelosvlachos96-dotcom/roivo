namespace Roivo.Application.Features.Businesses.Commands.ReactivateBusiness;

/// <summary>
/// Reactivates a previously-deactivated business, applying the new name/Kad/address
/// the user just typed (which may differ from the deactivated snapshot).
/// </summary>
public sealed record ReactivateBusinessCommand(
    Guid Id,
    string Name,
    string? Kad,
    string? Address);
