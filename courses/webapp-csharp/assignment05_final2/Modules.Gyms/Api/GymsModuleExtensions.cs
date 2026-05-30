using SharedKernel.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Modules.Gyms.Application;
using Modules.Gyms.Application.Authorization;
using Modules.Gyms.Application.Mappers;
using Modules.Gyms.Application.Persistence;
using Modules.Gyms.Application.Platform;
using Modules.Gyms.Infrastructure;
using Modules.Gyms.Infrastructure.Persistence;
using App.BLL.Contracts.Services;
using Shared.Contracts.Mediator.Diagnostics;
using Shared.Contracts.ModuleApis;

namespace Modules.Gyms.Api;

public static class GymsModuleExtensions
{
    /// <summary>
    /// Registers the Gyms module: MediatR handler discovery, the module's
    /// outward <see cref="IGymsModuleApi"/> contract (Phase 5), and the
    /// module assembly as an MVC <see cref="ApplicationPart"/> so the System
    /// and Tenant gym controllers are discovered when WebApp scans for
    /// controllers. The tenant-resolution middleware moved alongside the
    /// controllers; call <see cref="UseGymResolution"/> in the request
    /// pipeline.
    /// </summary>
    public static IServiceCollection AddGymsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GymsModuleMarker).Assembly));
        services.TryAddSingleton<IModuleEventRecorder, InMemoryModuleEventRecorder>();

        services.AddModuleDbContext<GymsDbContext>(configuration);
        services.AddScoped<IGymsModuleApi, GymsModuleApiService>();
        services.AddScoped<IAuthorizationQueryRepository, EfAuthorizationQueryRepository>();
        services.AddScoped<IWorkspaceContextService, WorkspaceContextService>();
        services.AddScoped<ITenantAccessChecker, TenantAccessChecker>();
        services.AddScoped<IResourceAuthorizationChecker, ResourceAuthorizationChecker>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<ISubscriptionTierLimitService, SubscriptionTierLimitService>();
        services.AddScoped<IPlatformService, PlatformService>();

        services.AddScoped<IGymsTenantRepository, EfGymsTenantRepository>();
        services.AddScoped<IGymsTenantPersistenceContext, EfGymsTenantPersistenceContext>();
        services.AddScoped<IGymsTenantMapper, GymsTenantMapper>();
        services.AddScoped<IGymsTenantWorkflowService, GymsTenantWorkflowService>();

        services.AddControllers()
            .AddApplicationPart(typeof(GymsModuleMarker).Assembly);

        return services;
    }

    /// <summary>
    /// Mounts the Gyms tenant-resolution middleware. Call after
    /// <c>UseAuthentication</c> and before <c>UseAuthorization</c> (the same
    /// position the legacy <c>WebApp.Middleware.GymResolutionMiddleware</c>
    /// occupied in Phase 4).
    /// </summary>
    public static IApplicationBuilder UseGymResolution(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<GymResolutionMiddleware>();
    }
}
