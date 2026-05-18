using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Enums;

namespace Roivo.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add/update</c> construct an
/// <see cref="ApplicationDbContext"/> without going through Program.cs's full
/// DI graph (which depends on services registered in feature projects).
/// Only used by EF Core tooling at design time.
/// </summary>
public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Roivo.Web"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string for design-time migration generation.");

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString);
        builder.UseOpenIddict<Guid>();

        return new ApplicationDbContext(builder.Options, new DesignTimeTenantContext());
    }

    /// <summary>No-op tenant context for design-time use only.</summary>
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid CurrentTenantId => Guid.Empty;
        public TenantType CurrentTenantType => TenantType.Business;
    }
}
