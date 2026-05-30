using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

/// <summary>
/// Aggregated book summary from AADE's RequestMyIncome endpoint. Represents
/// a daily summary of outgoing invoices grouped by counterparty and document
/// type. Unlike <see cref="Invoice"/> (individual incoming documents from
/// RequestDocs), IncomeBookEntry is aggregate-only by AADE's design — outgoing
/// invoices are exposed to the issuing entity only in summary form via this
/// endpoint.
///
/// Identity: composite of (BusinessId, CounterpartyAfm, IssueDate, DocumentTypeCode).
/// AADE returns one bookInfo element per unique tuple.
/// </summary>
public sealed class IncomeBookEntry : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }

    // Set by ApplicationDbContext.SaveChangesAsync override when unset; public
    // setter is required for ITenantScoped (same exception as Business/Invoice).
    public Guid TenantId { get; set; }

    /// <summary>
    /// VAT number of the counterparty. Typically a 9-character Greek AFM,
    /// but AADE returns counterparties from other EU countries with their
    /// own VAT number formats (up to ~12 characters). The column accommodates
    /// up to 20 characters for safety.
    /// </summary>
    public string CounterpartyAfm { get; private set; } = string.Empty;
    public DateTime IssueDate { get; private set; }
    public string DocumentTypeCode { get; private set; } = string.Empty;
    public decimal NetValue { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal GrossValue { get; private set; }
    public int InvoiceCount { get; private set; }
    public long MinMark { get; private set; }
    public long MaxMark { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public DateTime SyncedAt { get; private set; }

    private IncomeBookEntry() { } // EF Core

    public static IncomeBookEntry Create(
        Guid businessId,
        Guid tenantId,
        string counterpartyAfm,
        DateTime issueDate,
        string documentTypeCode,
        decimal netValue,
        decimal vatAmount,
        decimal grossValue,
        int invoiceCount,
        long minMark,
        long maxMark,
        string currency = "EUR")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(counterpartyAfm);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentTypeCode);
        if (invoiceCount < 0) throw new ArgumentException("invoiceCount cannot be negative");
        if (minMark > maxMark) throw new ArgumentException("minMark cannot exceed maxMark");

        return new IncomeBookEntry
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            TenantId = tenantId,
            CounterpartyAfm = counterpartyAfm,
            IssueDate = issueDate,
            DocumentTypeCode = documentTypeCode,
            NetValue = netValue,
            VatAmount = vatAmount,
            GrossValue = grossValue,
            InvoiceCount = invoiceCount,
            MinMark = minMark,
            MaxMark = maxMark,
            Currency = currency,
            SyncedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Updates totals when an existing aggregate changes (e.g., new invoices
    /// arrive that fall into the same date/counterparty/type bucket).
    /// </summary>
    public void UpdateTotals(
        decimal netValue,
        decimal vatAmount,
        decimal grossValue,
        int invoiceCount,
        long minMark,
        long maxMark)
    {
        if (invoiceCount < 0) throw new ArgumentException("invoiceCount cannot be negative");
        if (minMark > maxMark) throw new ArgumentException("minMark cannot exceed maxMark");

        NetValue = netValue;
        VatAmount = vatAmount;
        GrossValue = grossValue;
        InvoiceCount = invoiceCount;
        MinMark = minMark;
        MaxMark = maxMark;
        SyncedAt = DateTime.UtcNow;
    }
}
