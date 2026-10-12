using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class CashflowForecastConfiguration : IEntityTypeConfiguration<CashflowForecast>
{
    public void Configure(EntityTypeBuilder<CashflowForecast> builder)
    {
        foreach (var money in new[]
        {
            nameof(CashflowForecast.PredictedInflow),
            nameof(CashflowForecast.PredictedOutflow),
            nameof(CashflowForecast.PredictedBalance),
            nameof(CashflowForecast.ConfidenceLow),
            nameof(CashflowForecast.ConfidenceHigh),
            nameof(CashflowForecast.ActualInflow),
            nameof(CashflowForecast.ActualOutflow),
            nameof(CashflowForecast.ActualBalance),
        })
        {
            builder.Property(money).HasPrecision(18, 2);
        }

        builder.HasOne(f => f.Business).WithMany()
            .HasForeignKey(f => f.BusinessId).OnDelete(DeleteBehavior.Cascade);

        // One row per business per day: each nightly run replaces the window it
        // covers rather than appending a second opinion.
        builder.HasIndex(f => new { f.BusinessId, f.ForecastDate }).IsUnique();
    }
}
