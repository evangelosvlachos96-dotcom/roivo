namespace Roivo.Application.Features.Invoices.Queries.ListBusinessInvoicesPaged;

public enum InvoiceDirectionFilter { All, Incoming, Outgoing }
public enum CancelledFilter { All, HideCancelled, OnlyCancelled }

public sealed record ListBusinessInvoicesPagedQuery(
    Guid BusinessId,
    int Page,
    int PageSize,
    InvoiceDirectionFilter Direction = InvoiceDirectionFilter.All,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? CounterpartyAfmSearch = null,
    CancelledFilter CancelledStatus = CancelledFilter.All,
    string SortColumn = "IssueDate",
    bool SortDescending = true);
