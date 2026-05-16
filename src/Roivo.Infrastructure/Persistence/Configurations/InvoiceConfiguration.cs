using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(i => i.Currency)
            .HasConversion<string>()
            .HasMaxLength(3);

        builder.Property(i => i.Direction)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(i => new { i.TenantId, i.IssueDate });
        builder.HasIndex(i => new { i.TenantId, i.AadeMark }).IsUnique();
    }
}
