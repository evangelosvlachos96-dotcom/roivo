using Roivo.Core.Domain.Enums;
using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

public class BankTransaction : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public required string ExternalId { get; set; }
    public DateOnly BookingDate { get; set; }
    public DateOnly? ValueDate { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; } = Currency.EUR;
    public string? CounterpartyName { get; set; }
    public string? CounterpartyIban { get; set; }
    public string? Reference { get; set; }
    public string? RawPayload { get; set; }
    public Guid? MatchedInvoiceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
