using Roivo.Application.Abstractions;
using Roivo.Application.Features.Cashflow.Services;

namespace Roivo.Application.Features.Cashflow.Queries.GetTaxCalendar;

public sealed class GetTaxCalendarHandler
{
    private readonly ICashflowRepository _repository;
    private readonly IGreekTaxCalendar _calendar;

    public GetTaxCalendarHandler(ICashflowRepository repository, IGreekTaxCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(calendar);

        _repository = repository;
        _calendar = calendar;
    }

    public async Task<TaxCalendar> Handle(
        GetTaxCalendarQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.To < query.From)
            return new TaxCalendar(query.From, query.To, []);

        // Generate first, then persist the ones that are new: the upsert matches
        // on (business, type, period), so a payment already recorded against an
        // obligation survives a regeneration.
        var generated = await _calendar
            .GenerateAsync(query.BusinessId, query.From, query.To, cancellationToken)
            .ConfigureAwait(false);

        // Only obligations that have not yet fallen due are persisted. Viewing a
        // past quarter must not manufacture unpaid rows for periods that are
        // over: nobody will ever mark them paid, so they would render as
        // permanently overdue, and paging back through years would write a row
        // per period per visit. Historical quarters still display whatever is
        // genuinely on file.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var persistable = generated.Where(o => o.DueDate >= today).ToList();

        if (persistable.Count > 0)
            await _repository.UpsertTaxObligationsAsync(persistable, cancellationToken).ConfigureAwait(false);

        var stored = await _repository
            .ListTaxObligationsAsync(query.BusinessId, query.From, query.To, cancellationToken)
            .ConfigureAwait(false);

        return new TaxCalendar(query.From, query.To, [.. stored.OrderBy(o => o.DueDate).ThenBy(o => o.TaxType)]);
    }
}
