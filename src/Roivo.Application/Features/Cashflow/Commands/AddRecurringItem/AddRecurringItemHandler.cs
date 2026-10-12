using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Exceptions;

namespace Roivo.Application.Features.Cashflow.Commands.AddRecurringItem;

public sealed class AddRecurringItemHandler
{
    private readonly ICashflowRepository _repository;
    private readonly IBusinessRepository _businesses;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public AddRecurringItemHandler(
        ICashflowRepository repository,
        IBusinessRepository businesses,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _repository = repository;
        _businesses = businesses;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<AddRecurringItemResult> Handle(
        AddRecurringItemCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanManageCashflowFor(_tenant.CurrentTenantType))
            return new AddRecurringItemResult.Forbidden("Δεν επιτρέπεται η διαχείριση ταμειακών ροών από αυτόν τον τύπο λογαριασμού.");

        var business = await _businesses.GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken);
        if (business is null)
            return new AddRecurringItemResult.NotFound();

        CashflowCategory category;
        try
        {
            category = CashflowCategory.Create(
                command.BusinessId,
                command.Name,
                command.Type,
                isRecurring: command.RecurringDay.HasValue,
                recurringDay: command.RecurringDay,
                averageAmount: command.Amount);
        }
        catch (DomainException ex)
        {
            return new AddRecurringItemResult.Invalid(ex.Message);
        }

        await _repository.AddCategoryAsync(category, cancellationToken);

        await _audit.WriteAsync(
            action: AuditAction.CashflowCategoryCreated,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(CashflowCategory),
            entityId: category.Id.ToString(),
            details: new Dictionary<string, object?>
            {
                ["BusinessId"] = command.BusinessId,
                ["Name"] = category.Name,
                ["Type"] = category.Type.ToString(),
                ["RecurringDay"] = category.RecurringDay,
                ["Amount"] = category.AverageAmount,
            },
            cancellationToken: cancellationToken);

        return new AddRecurringItemResult.Success(category.Id);
    }
}
