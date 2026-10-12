namespace Roivo.Core.Domain.Enums;

/// <summary>How a <see cref="Entities.ReconciliationMatch"/> came to exist.</summary>
public enum ReconciliationMatchType
{
    /// <summary>Scored at or above the automatic threshold and confirmed without review.</summary>
    Automatic = 1,

    /// <summary>Created by a user pairing an invoice with a transaction by hand.</summary>
    Manual = 2,

    /// <summary>Scored above the suggestion threshold but below automatic; awaits review.</summary>
    Suggested = 3,
}
