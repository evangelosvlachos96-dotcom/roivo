using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Roivo.Core.Domain;
using Roivo.Infrastructure.MultiTenancy;

namespace Roivo.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.UseOpenIddict<Guid>();

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { builder });
            }
        }

        builder.Entity<Tenant>().Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
        builder.Entity<BankAccount>().Property(b => b.Currency).HasConversion<string>().HasMaxLength(3);
        builder.Entity<BankTransaction>().Property(t => t.Currency).HasConversion<string>().HasMaxLength(3);
        builder.Entity<Invoice>().Property(i => i.Currency).HasConversion<string>().HasMaxLength(3);
        builder.Entity<Invoice>().Property(i => i.Direction).HasConversion<string>().HasMaxLength(20);
        builder.Entity<Invoice>().Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.Entity<BankTransaction>()
            .HasIndex(x => new { x.TenantId, x.BookingDate });

        builder.Entity<Invoice>()
            .HasIndex(x => new { x.TenantId, x.IssueDate });
        builder.Entity<Invoice>()
            .HasIndex(x => new { x.TenantId, x.AadeMark })
            .IsUnique();

        builder.Entity<Business>()
            .HasIndex(x => new { x.TenantId, x.Afm })
            .IsUnique();
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantScoped
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantContext.CurrentTenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_tenantContext.CurrentTenantId is Guid tenantId)
        {
            foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
            {
                if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                {
                    entry.Entity.TenantId = tenantId;
                }
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
