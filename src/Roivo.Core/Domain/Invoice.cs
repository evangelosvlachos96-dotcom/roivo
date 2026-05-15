namespace Roivo.Core.Domain;

public class Invoice : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }
    public required string AadeMark { get; set; }
    public string? AadeUid { get; set; }
    public string? AadeQrUrl { get; set; }
    public InvoiceDirection Direction { get; set; }
    public required string InvoiceType { get; set; }
    public string? Series { get; set; }
    public int Number { get; set; }
    public DateOnly IssueDate { get; set; }
    public required string CounterpartyAfm { get; set; }
    public string? CounterpartyName { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public Currency Currency { get; set; } = Currency.EUR;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RawPayload { get; set; }
}

public enum InvoiceDirection
{
    Issued = 1,
    Received = 2
}

public enum InvoiceStatus
{
    Open = 1,
    Paid = 2,
    PartiallyPaid = 3,
    Overdue = 4,
    Cancelled = 5
}
