namespace Roivo.Core.Domain.Enums;

/// <summary>Review state of a <see cref="Entities.ReconciliationMatch"/>.</summary>
public enum ReconciliationMatchStatus
{
    /// <summary>Awaiting a user decision. Does not mark either side reconciled.</summary>
    Pending = 1,

    /// <summary>Accepted. Both the invoice and the transaction count as reconciled.</summary>
    Confirmed = 2,

    /// <summary>Declined by a user. Retained so the engine does not re-suggest the pair.</summary>
    Rejected = 3,
}
