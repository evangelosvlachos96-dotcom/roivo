using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Aade.Commands.SyncBusinessInvoices;

public sealed class SyncBusinessInvoicesHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IInvoiceRepository _invoices;
    private readonly IAadeClient _aade;
    private readonly IAadeCredentialStore _credentials;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public SyncBusinessInvoicesHandler(
        IBusinessRepository businesses,
        IInvoiceRepository invoices,
        IAadeClient aade,
        IAadeCredentialStore credentials,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(invoices);
        ArgumentNullException.ThrowIfNull(aade);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _businesses = businesses;
        _invoices = invoices;
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

        var fetch = await _aade.FetchInvoicesAsync(creds.UserId, creds.SubscriptionKey, business.LastAadeSyncAt, cancellationToken).ConfigureAwait(false);
        switch (fetch)
        {
            case AadeFetchResult.InvalidCredentials:
                return new SyncBusinessInvoicesResult.InvalidCredentials();
            case AadeFetchResult.NetworkError ne:
                return new SyncBusinessInvoicesResult.NetworkError(ne.Message);
            case AadeFetchResult.AadeServerError se:
                return new SyncBusinessInvoicesResult.AadeServerError(se.StatusCode, se.Message);
        }

        var success = (AadeFetchResult.Success)fetch;

        var newCount = 0;
        var updatedCount = 0;

        await UpsertBatchAsync(success.Outgoing, business.Id, InvoiceDirection.Issued, isNew => { if (isNew) newCount++; else updatedCount++; }, cancellationToken).ConfigureAwait(false);
        await UpsertBatchAsync(success.Incoming, business.Id, InvoiceDirection.Received, isNew => { if (isNew) newCount++; else updatedCount++; }, cancellationToken).ConfigureAwait(false);

        business.RecordAadeSync(DateTime.UtcNow);
        await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            action: "AadeSyncCompleted",
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, business.Afm, NewInvoices = newCount, UpdatedInvoices = updatedCount },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new SyncBusinessInvoicesResult.Success(newCount, updatedCount);
    }

    private async Task UpsertBatchAsync(
        IReadOnlyList<AadeInvoiceDto> dtos,
        Guid businessId,
        InvoiceDirection direction,
        Action<bool> recordOutcome,
        CancellationToken cancellationToken)
    {
        foreach (var dto in dtos)
        {
            var existing = await _invoices.GetByAadeMarkAsync(dto.Mark, cancellationToken).ConfigureAwait(false);
            var isNew = existing is null;

            var entity = existing ?? new Invoice
            {
                BusinessId = businessId,
                AadeMark = dto.Mark,
                CounterpartyAfm = dto.CounterpartyAfm,
                InvoiceType = dto.DocumentTypeCode,
            };

            entity.BusinessId = businessId;
            entity.CounterpartyAfm = dto.CounterpartyAfm;
            entity.CounterpartyName = dto.CounterpartyName;
            entity.IssueDate = DateOnly.FromDateTime(dto.IssueDate);
            entity.InvoiceType = dto.DocumentTypeCode;
            entity.GrossAmount = dto.GrossAmount;
            entity.Currency = ParseCurrency(dto.Currency);
            entity.Direction = direction;
            entity.RawPayload = dto.RawXml;

            await _invoices.UpsertAsync(entity, cancellationToken).ConfigureAwait(false);
            recordOutcome(isNew);
        }
    }

    private static Currency ParseCurrency(string code)
        => Enum.TryParse<Currency>(code, ignoreCase: true, out var parsed) ? parsed : Currency.EUR;
}
