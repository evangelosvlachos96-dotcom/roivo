using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.Property(n => n.UserId).HasMaxLength(450);
        builder.Property(n => n.CashflowAlertThreshold).HasPrecision(18, 2);

        builder.HasOne(n => n.Business).WithMany()
            .HasForeignKey(n => n.BusinessId).OnDelete(DeleteBehavior.Cascade);

        // Preferences are per user per business, and the jobs look them up by
        // exactly that pair, so the uniqueness constraint is also the index
        // they need.
        builder.HasIndex(n => new { n.BusinessId, n.UserId }).IsUnique();

        // The nightly notification jobs scan by user across businesses.
        builder.HasIndex(n => n.UserId);
    }
}
