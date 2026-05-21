using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public sealed class IncomeBookEntryConfiguration : IEntityTypeConfiguration<IncomeBookEntry>
{
    public void Configure(EntityTypeBuilder<IncomeBookEntry> builder)
    {
        builder.ToTable("IncomeBookEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BusinessId).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        // 20 chars: usually a 9-char Greek AFM, but AADE returns EU counterparties
        // with longer VAT-number formats. See entity comment.
        builder.Property(e => e.CounterpartyAfm).IsRequired().HasMaxLength(20);
        builder.Property(e => e.IssueDate).IsRequired();
        builder.Property(e => e.DocumentTypeCode).IsRequired().HasMaxLength(10);
        builder.Property(e => e.NetValue).HasPrecision(18, 2);
        builder.Property(e => e.VatAmount).HasPrecision(18, 2);
        builder.Property(e => e.GrossValue).HasPrecision(18, 2);
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(3);
        builder.Property(e => e.SyncedAt).IsRequired();

        // Composite unique constraint — AADE aggregates by this exact tuple.
        builder.HasIndex(e => new { e.BusinessId, e.CounterpartyAfm, e.IssueDate, e.DocumentTypeCode })
               .IsUnique();

        builder.HasIndex(e => e.TenantId);

        // Tenant scoping: IncomeBookEntry implements ITenantScoped, so the global
        // query filter is applied centrally in ApplicationDbContext.ApplyTenantFilters —
        // the same mechanism BusinessConfiguration relies on. No explicit filter here.
    }
}
