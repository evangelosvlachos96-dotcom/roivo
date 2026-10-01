using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.Property(a => a.Currency)
            .HasConversion<string>()
            .HasMaxLength(3);

        builder.Property(a => a.BankName).HasMaxLength(200);
        builder.Property(a => a.Iban).HasMaxLength(34);
        builder.Property(a => a.ExternalAccountUid).HasMaxLength(200);

        // Sync looks accounts up by the aggregator's uid, and the uid is the
        // natural key we upsert on — one row per account per business.
        builder.HasIndex(a => new { a.BusinessId, a.ExternalAccountUid }).IsUnique();
    }
}
