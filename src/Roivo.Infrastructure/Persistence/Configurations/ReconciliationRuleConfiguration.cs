using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class ReconciliationRuleConfiguration : IEntityTypeConfiguration<ReconciliationRule>
{
    public void Configure(EntityTypeBuilder<ReconciliationRule> builder)
    {
        builder.Property(r => r.RuleName).HasMaxLength(200);
        builder.Property(r => r.CounterpartyPattern).HasMaxLength(500);
        builder.Property(r => r.AmountTolerancePercent).HasPrecision(6, 3);

        builder.HasOne(r => r.Business).WithMany()
            .HasForeignKey(r => r.BusinessId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.BusinessId, r.IsActive });
    }
}
