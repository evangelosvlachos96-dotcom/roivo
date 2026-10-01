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

        builder.Property(t => t.ExternalId).HasMaxLength(200);
        builder.Property(t => t.CounterpartyName).HasMaxLength(200);
        builder.Property(t => t.CounterpartyIban).HasMaxLength(34);
        builder.Property(t => t.Reference).HasMaxLength(500);

        builder.HasIndex(t => new { t.TenantId, t.BookingDate });

        // Sync is idempotent by (account, bank's own reference); the unique
        // index makes a double-insert impossible rather than merely unlikely.
        builder.HasIndex(t => new { t.BankAccountId, t.ExternalId }).IsUnique();
    }
}
