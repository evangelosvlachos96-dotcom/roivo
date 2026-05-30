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
    }
}
