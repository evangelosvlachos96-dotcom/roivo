using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Configurations;

public class ReconciliationMatchConfiguration : IEntityTypeConfiguration<ReconciliationMatch>
{
    public void Configure(EntityTypeBuilder<ReconciliationMatch> builder)
    {
        builder.Property(m => m.MatchType).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.MatchConfidence).HasPrecision(5, 4);
        builder.Property(m => m.MatchedByUserId).HasMaxLength(450);
        builder.Property(m => m.Notes).HasMaxLength(1000);

        builder.HasOne(m => m.Business).WithMany()
            .HasForeignKey(m => m.BusinessId).OnDelete(DeleteBehavior.Cascade);

        // Restrict rather than Cascade: losing an invoice should not silently
        // erase the audit trail of how it was reconciled.
        builder.HasOne(m => m.Invoice).WithMany()
            .HasForeignKey(m => m.InvoiceId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.BankTransaction).WithMany()
            .HasForeignKey(m => m.BankTransactionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.BusinessId, m.Status });
        builder.HasIndex(m => m.InvoiceId);
        builder.HasIndex(m => m.BankTransactionId);

        // One live match per pair. Rejected rows are excluded so a pair can be
        // re-matched by hand after the engine guessed wrong, while the rejection
        // itself is still on record.
        builder.HasIndex(m => new { m.InvoiceId, m.BankTransactionId })
            .IsUnique()
            .HasFilter("\"Status\" <> 'Rejected'");
    }
}
