using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class TaxObligationConfiguration : IEntityTypeConfiguration<TaxObligation>
{
    public void Configure(EntityTypeBuilder<TaxObligation> builder)
    {
        builder.Property(t => t.TaxType).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.Period).HasMaxLength(20);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.Property(t => t.EstimatedAmount).HasPrecision(18, 2);
        builder.Property(t => t.ActualAmount).HasPrecision(18, 2);

        builder.HasOne(t => t.Business).WithMany()
            .HasForeignKey(t => t.BusinessId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.BusinessId, t.DueDate });

        // Identity of an obligation is the period it settles, which is what
        // makes regenerating the calendar idempotent.
        builder.HasIndex(t => new { t.BusinessId, t.TaxType, t.Period }).IsUnique();
    }
}
