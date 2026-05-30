using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Aade.Commands.SyncBusinessInvoices;

public sealed class SyncBusinessInvoicesHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IInvoiceRepository _invoices;
    private readonly IIncomeBookEntryRepository _bookEntries;
    private readonly IAadeClient _aade;
    private readonly IAadeCredentialStore _credentials;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public SyncBusinessInvoicesHandler(
        IBusinessRepository businesses,
        IInvoiceRepository invoices,
        IIncomeBookEntryRepository bookEntries,
        IAadeClient aade,
        IAadeCredentialStore credentials,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(invoices);
        ArgumentNullException.ThrowIfNull(bookEntries);
        ArgumentNullException.ThrowIfNull(aade);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _businesses = businesses;
        _invoices = invoices;
        _bookEntries = bookEntries;
        _aade = aade;
        _credentials = credentials;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<SyncBusinessInvoicesResult> Handle(SyncBusinessInvoicesCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var business = await _businesses.GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return new SyncBusinessInvoicesResult.BusinessNotFound();

        var creds = await _credentials.RetrieveAsync(business.Id, cancellationToken).ConfigureAwait(false);
        if (creds is null)
            return new SyncBusinessInvoicesResult.NotConnected();

        var sinceIncoming = business.LastAadeIncomingMark ?? 0;
        var sinceOutgoing = business.LastAadeOutgoingMark ?? 0;

        var fetch = await _aade.FetchInvoicesAsync(creds.UserId, creds.SubscriptionKey, sinceIncoming, sinceOutgoing, cancellationToken).ConfigureAwait(false);
        switch (fetch)
        {
            case AadeFetchResult.InvalidCredentials:
                // Persistent auth failure — mark so the banner shows and the
                // 24-hour notification cron picks it up. Network/server errors
                // below are transient and intentionally do NOT mark.
                business.RecordAadeSyncFailure("InvalidCredentials");
                await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);
                return new SyncBusinessInvoicesResult.InvalidCredentials();
            case AadeFetchResult.NetworkError ne:
                return new SyncBusinessInvoicesResult.NetworkError(ne.Message);
            case AadeFetchResult.AadeServerError se:
                return new SyncBusinessInvoicesResult.AadeServerError(se.StatusCode, se.Message);
        }

        var success = (AadeFetchResult.Success)fetch;
        var tenantId = _tenant.CurrentTenantId;

        // Heal failure state before applying sync progress — if the connection
        // was previously broken and is now working, clear the banner.
        business.ClearAadeSyncFailure();

        // Incoming documents (RequestDocs) — individual invoices.
        foreach (var dto in success.Incoming)
        {
            var invoice = Invoice.Create(
                businessId: business.Id,
                aadeMark: dto.Mark,
                direction: InvoiceDirection.Received,
                invoiceType: dto.DocumentTypeCode,
                issueDate: DateOnly.FromDateTime(dto.IssueDate),
                counterpartyAfm: dto.CounterpartyAfm,
                counterpartyName: dto.CounterpartyName,
                netAmount: dto.NetAmount,
                vatAmount: dto.VatAmount,
                grossAmount: dto.GrossAmount,
                currency: ParseCurrency(dto.Currency),
                cancelledByMark: dto.CancelledByMark);

            await _invoices.UpsertAsync(invoice, cancellationToken).ConfigureAwait(false);
        }

        // Outgoing summaries (RequestMyIncome) — aggregated book entries.
        foreach (var dto in success.Outgoing)
        {
            var existing = await _bookEntries.GetByIdentityAsync(
                business.Id, dto.CounterpartyAfm, dto.IssueDate, dto.DocumentTypeCode, cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                existing.UpdateTotals(dto.NetValue, dto.VatAmount, dto.GrossValue, dto.InvoiceCount, dto.MinMark, dto.MaxMark);
            }
            else
            {
                existing = IncomeBookEntry.Create(
                    businessId: business.Id,
                    tenantId: tenantId,
                    counterpartyAfm: dto.CounterpartyAfm,
                    issueDate: dto.IssueDate,
                    documentTypeCode: dto.DocumentTypeCode,
                    netValue: dto.NetValue,
                    vatAmount: dto.VatAmount,
                    grossValue: dto.GrossValue,
                    invoiceCount: dto.InvoiceCount,
                    minMark: dto.MinMark,
                    maxMark: dto.MaxMark);
            }

            await _bookEntries.UpsertAsync(existing, cancellationToken).ConfigureAwait(false);
        }

        business.RecordAadeSyncProgress(
            newIncomingMark: success.MaxIncomingMark,
            newOutgoingMark: success.MaxOutgoingMark);
        await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            action: AuditAction.AadeSyncCompleted,
            tenantId: tenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new
            {
                business.Name,
                business.Afm,
                IncomingInvoices = success.Incoming.Count,
                OutgoingBookEntries = success.Outgoing.Count,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new SyncBusinessInvoicesResult.Success(
            NewCount: success.Incoming.Count,
            UpdatedCount: 0);
    }

    private static Currency ParseCurrency(string code)
        => Enum.TryParse<Currency>(code, ignoreCase: true, out var parsed) ? parsed : Currency.EUR;
}
