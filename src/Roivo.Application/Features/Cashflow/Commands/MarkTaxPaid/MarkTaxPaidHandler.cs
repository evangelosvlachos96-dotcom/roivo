using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Exceptions;

namespace Roivo.Application.Features.Cashflow.Commands.MarkTaxPaid;

public sealed class MarkTaxPaidHandler
{
    private readonly ICashflowRepository _repository;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public MarkTaxPaidHandler(
        ICashflowRepository repository,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _repository = repository;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<MarkTaxPaidResult> Handle(
        MarkTaxPaidCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanManageCashflowFor(_tenant.CurrentTenantType))
            return new MarkTaxPaidResult.Forbidden("Δεν επιτρέπεται η διαχείριση φορολογικών υποχρεώσεων από αυτόν τον τύπο λογαριασμού.");

        if (command.ActualAmount < 0m)
            return new MarkTaxPaidResult.InvalidAmount();

        var obligation = await _repository.GetTaxObligationByIdAsync(command.TaxObligationId, cancellationToken);
        if (obligation is null)
            return new MarkTaxPaidResult.NotFound();

        try
        {
            obligation.MarkPaid(command.ActualAmount, command.PaidAtUtc);
        }
        catch (DomainException)
        {
            return new MarkTaxPaidResult.AlreadyPaid();
        }

        await _repository.UpdateTaxObligationAsync(obligation, cancellationToken);

        await _audit.WriteAsync(
            action: AuditAction.TaxObligationMarkedPaid,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(TaxObligation),
            entityId: obligation.Id.ToString(),
            details: new Dictionary<string, object?>
            {
                ["TaxType"] = obligation.TaxType.ToString(),
                ["Period"] = obligation.Period,
                ["Estimated"] = obligation.EstimatedAmount,
                ["Actual"] = command.ActualAmount,
            },
            cancellationToken: cancellationToken);

        return new MarkTaxPaidResult.Success();
    }
}
