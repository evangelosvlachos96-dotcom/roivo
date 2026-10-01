using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.Property(b => b.IsActive)
             .HasDefaultValue(true);
        builder.HasIndex(b => new { b.TenantId, b.Afm }).IsUnique();

        builder.Property(b => b.LastAadeIncomingMark);
        builder.Property(b => b.LastAadeOutgoingMark);

        builder.Property(b => b.HasAadeFailure)
            .HasDefaultValue(false);
        builder.Property(b => b.AadeLastFailureAt);
        builder.Property(b => b.AadeLastFailureReason)
            .HasMaxLength(50);
        builder.Property(b => b.AadeFailureEmailSentAt);

        builder.Property(b => b.BankingProviderName)
            .HasMaxLength(200);
        builder.Property(b => b.BankingSyncErrorCount)
            .HasDefaultValue(0);
        builder.Property(b => b.BankingLastFailureReason)
            .HasMaxLength(50);

        // The nightly cron scans every tenant for connected businesses; without
        // this it is a full table scan of Businesses on every run.
        builder.HasIndex(b => b.BankingAccessTokenEncrypted)
            .HasFilter("\"BankingAccessTokenEncrypted\" IS NOT NULL");
    }
}
