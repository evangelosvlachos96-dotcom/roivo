using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.Timestamp)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.HasIndex(a => new { a.TenantId, a.Timestamp });
        builder.HasIndex(a => new { a.UserId, a.Timestamp });
    }
}
