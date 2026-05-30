using Microsoft.Extensions.DependencyInjection;
using Roivo.Application.Features.Businesses.Commands.CreateBusiness;
using Roivo.Application.Features.Businesses.Commands.DeactivateBusiness;
using Roivo.Application.Features.Businesses.Commands.ReactivateBusiness;
using Roivo.Application.Features.Businesses.Commands.UpdateBusiness;
using Roivo.Application.Features.Businesses.Queries.GetBusinessById;
using Roivo.Application.Features.Businesses.Queries.ListActiveBusinesses;
using Roivo.Application.Features.Aade.Commands.ConnectAade;
using Roivo.Application.Features.Aade.Commands.DisconnectAade;
using Roivo.Application.Features.Aade.Commands.SyncBusinessInvoices;
using Roivo.Application.Features.Aade.Queries.GetAadeConnectionStatus;
using Roivo.Application.Features.Aade.Queries.ListConnectedBusinesses;
using Roivo.Application.Features.Invoices.Queries.GetBusinessInvoiceSummary;
using Roivo.Application.Features.Invoices.Queries.ListBusinessInvoicesPaged;
using Roivo.Application.Features.Tenants.Queries.CountAllTenants;
using Roivo.Application.Features.Tenants.Queries.GetCurrentTenant;

namespace Roivo.Application.Configuration;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers Application-layer services (command/query handlers).
    /// Infrastructure-side bindings (repositories, audit writer, tenant context)
    /// are registered separately by Infrastructure's DI extensions.
    /// </summary>
    public static IServiceCollection AddRoivoApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<CreateBusinessHandler>();
        services.AddScoped<UpdateBusinessHandler>();
        services.AddScoped<DeactivateBusinessHandler>();
        services.AddScoped<ReactivateBusinessHandler>();
        services.AddScoped<GetBusinessByIdHandler>();
        services.AddScoped<ListActiveBusinessesHandler>();
        services.AddScoped<GetCurrentTenantHandler>();
        services.AddScoped<CountAllTenantsHandler>();

        services.AddScoped<ConnectAadeHandler>();
        services.AddScoped<DisconnectAadeHandler>();
        services.AddScoped<SyncBusinessInvoicesHandler>();
        services.AddScoped<GetAadeConnectionStatusHandler>();
        services.AddScoped<ListConnectedBusinessesHandler>();

        services.AddScoped<GetBusinessInvoiceSummaryHandler>();
        services.AddScoped<ListBusinessInvoicesPagedHandler>();

        return services;
    }
}
