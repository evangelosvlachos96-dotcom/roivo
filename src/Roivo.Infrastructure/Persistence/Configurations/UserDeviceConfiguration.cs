using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public sealed class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("UserDevices");
        builder.HasKey(d => d.Id);

        // SHA-256 as lowercase hex is always 64 characters.
        builder.Property(d => d.DeviceHash).HasMaxLength(64).IsRequired();
        builder.Property(d => d.DeviceName).HasMaxLength(120).IsRequired();

        // Long enough for four groups of IPv6; IPv4 prefixes are far shorter.
        builder.Property(d => d.IpPrefix).HasMaxLength(64);

        builder.Property(d => d.FirstSeenAt).IsRequired();
        builder.Property(d => d.LastSeenAt).IsRequired();

        // The lookup on every sign-in is (user, hash), and it must be unique:
        // without this a race between two sign-ins on one new device inserts
        // the row twice and notifies the owner twice.
        builder.HasIndex(d => new { d.UserId, d.DeviceHash }).IsUnique();

        // Forgetting the account forgets its devices.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
