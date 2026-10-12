using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class CashflowCategoryConfiguration : IEntityTypeConfiguration<CashflowCategory>
{
    public void Configure(EntityTypeBuilder<CashflowCategory> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(200);
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.AverageAmount).HasPrecision(18, 2);

        builder.HasOne(c => c.Business).WithMany()
            .HasForeignKey(c => c.BusinessId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.BusinessId, c.Type });
    }
}
