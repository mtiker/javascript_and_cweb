using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using App.BLL.Contracts.Services;
using SharedKernel.Persistence;
using Modules.Maintenance.Application;
using Modules.Maintenance.Application.Mappers;
using Modules.Maintenance.Application.Persistence;
using Modules.Maintenance.Infrastructure;
using Modules.Maintenance.Infrastructure.Persistence;
using Shared.Contracts.Mediator.Diagnostics;

namespace Modules.Maintenance.Api;

public static class MaintenanceModuleExtensions
{
    /// <summary>
    /// Registers the Maintenance module: MediatR handler discovery,
    /// module-owned maintenance repository/workflow services (Phase 8), and
    /// the module assembly as an MVC <see cref="ApplicationPart"/> so the
    /// relocated equipment and maintenance controllers are discovered by
    /// WebApp.
    /// </summary>
    public static IServiceCollection AddMaintenanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MaintenanceModuleMarker).Assembly));
        services.TryAddSingleton<IModuleEventRecorder, InMemoryModuleEventRecorder>();

        services.AddModuleDbContext<MaintenanceDbContext>(configuration);
        services.AddScoped<IMaintenanceRepository, EfMaintenanceRepository>();
        services.AddScoped<IMaintenancePersistenceContext, EfMaintenancePersistenceContext>();
        services.AddScoped<IMaintenanceMapper, MaintenanceMapper>();
        services.AddScoped<IMaintenanceWorkflowService, MaintenanceWorkflowService>();

        services.AddControllers()
            .AddApplicationPart(typeof(MaintenanceModuleMarker).Assembly);

        return services;
    }
}
