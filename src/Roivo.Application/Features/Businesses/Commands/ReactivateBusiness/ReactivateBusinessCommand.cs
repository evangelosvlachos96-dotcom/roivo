namespace Roivo.Application.Features.Businesses.Commands.ReactivateBusiness;

/// <summary>
/// Reactivates a previously-deactivated business, restoring its prior name,
/// KAD, and address exactly. Edits happen via the normal update flow afterward.
/// </summary>
public sealed record ReactivateBusinessCommand(Guid Id);
