using Roivo.Core.Domain.Enums;
using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

/// <summary>
/// An individual incoming document fetched from AADE's RequestDocs endpoint.
/// Outgoing documents are aggregate-only and modelled by <see cref="IncomeBookEntry"/>.
/// </summary>
public class Invoice : ITenantScoped
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // Set automatically by ApplicationDbContext.SaveChangesAsync override; public
    // setter is the one exception to the private-setter rule (same as Business).
    public Guid TenantId { get; set; }

    public Guid BusinessId { get; private set; }
    public Business? Business { get; private set; }
    public string AadeMark { get; private set; } = null!;
    public string? AadeUid { get; private set; }
    public string? AadeQrUrl { get; private set; }
    public InvoiceDirection Direction { get; private set; }
    public string InvoiceType { get; private set; } = null!;
    public string? Series { get; private set; }
    public int Number { get; private set; }
    public DateOnly IssueDate { get; private set; }
    /// <summary>
    /// VAT number of the counterparty. Typically a 9-character Greek AFM,
    /// but AADE returns counterparties from other EU countries with their
    /// own VAT number formats (up to ~12 characters). The column accommodates
    /// up to 20 characters for safety.
    /// </summary>
    public string CounterpartyAfm { get; private set; } = null!;
    public string? CounterpartyName { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal GrossAmount { get; private set; }
    public Currency Currency { get; private set; } = Currency.EUR;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Open;
    public DateTime? PaidAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Mark of the cancellation document that voided this invoice, if any.</summary>
    public string? CancelledByMark { get; private set; }

    // Required for EF Core materialization. Not callable externally — use Invoice.Create(...).
    private Invoice() { }

    /// <summary>
    /// Creates an invoice from a parsed AADE incoming document.
    /// </summary>
    public static Invoice Create(
        Guid businessId,
        string aadeMark,
        InvoiceDirection direction,
        string invoiceType,
        DateOnly issueDate,
        string counterpartyAfm,
        string? counterpartyName,
        decimal netAmount,
        decimal vatAmount,
        decimal grossAmount,
        Currency currency,
        string? cancelledByMark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aadeMark);
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(counterpartyAfm);

        return new Invoice
        {
            BusinessId = businessId,
            AadeMark = aadeMark,
            Direction = direction,
            InvoiceType = invoiceType,
            IssueDate = issueDate,
            CounterpartyAfm = counterpartyAfm,
            CounterpartyName = counterpartyName,
            NetAmount = netAmount,
            VatAmount = vatAmount,
            GrossAmount = grossAmount,
            Currency = currency,
            CancelledByMark = string.IsNullOrWhiteSpace(cancelledByMark) ? null : cancelledByMark,
        };
    }

    /// <summary>
    /// Records that this invoice was cancelled by a subsequent cancellation document.
    /// Cancelled invoices are typically excluded from cashflow calculations.
    /// </summary>
    public void RecordCancellation(string cancelledByMark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cancelledByMark);
        CancelledByMark = cancelledByMark;
    }
}
