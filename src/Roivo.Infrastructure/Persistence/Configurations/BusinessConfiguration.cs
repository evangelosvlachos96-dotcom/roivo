using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        // TODO: AADE credential encryption setup (M4) — currently stored as plaintext placeholders.
        builder.HasIndex(b => new { b.TenantId, b.Afm }).IsUnique();
    }
}
