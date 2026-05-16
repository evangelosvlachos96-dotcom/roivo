using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.Property(t => t.Currency)
            .HasConversion<string>()
            .HasMaxLength(3);

        builder.HasIndex(t => new { t.TenantId, t.BookingDate });
    }
}
