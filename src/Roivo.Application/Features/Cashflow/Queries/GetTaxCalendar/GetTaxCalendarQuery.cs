using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Cashflow.Queries.GetTaxCalendar;

public sealed record GetTaxCalendarQuery(Guid BusinessId, DateOnly From, DateOnly To);

/// <summary>Stored obligations for the window, plus any the calendar says are missing.</summary>
public sealed record TaxCalendar(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<TaxObligation> Obligations);
