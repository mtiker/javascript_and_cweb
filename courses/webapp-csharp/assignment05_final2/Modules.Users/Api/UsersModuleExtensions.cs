using App.BLL.Contracts.Services;
using SharedKernel.Persistence;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Modules.Users.Application;
using Modules.Users.Application.Auth;
using Modules.Users.Application.Persistence;
using Modules.Users.Application.Mappers;
using Modules.Users.Infrastructure;
using Modules.Users.Infrastructure.Persistence;
using Shared.Contracts.Mediator.Diagnostics;
using Shared.Contracts.ModuleApis;

namespace Modules.Users.Api;

public static class UsersModuleExtensions
{
    /// <summary>
    /// Registers the Users module: MediatR handler discovery, the module's
    /// outward <see cref="IUsersModuleApi"/> contract, the internal
    /// <see cref="IUsersAuthService"/> facade used by
    /// <see cref="AccountController"/>, and Users-owned persistence (refresh
    /// tokens, Phase 4). Also adds the module assembly to MVC's
    /// <see cref="ApplicationPartManager"/> so the controller is discovered
    /// when WebApp scans for controllers.
    /// </summary>
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(UsersModuleMarker).Assembly));
        services.TryAddSingleton<IModuleEventRecorder, InMemoryModuleEventRecorder>();

        services.AddModuleDbContext<UsersDbContext>(configuration);
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddScoped<IUsersAuthService, UsersAuthService>();
        services.AddScoped<IUsersModuleApi, UsersModuleApiService>();

        // Phase 10c (Users slice): BLL implementations physically owned by
        // this module. Interfaces still live in App.BLL.Contracts until a
        // later sub-phase relocates them.
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<ICurrentActorResolver, CurrentActorResolver>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthResponseMapper, AuthResponseMapper>();
        services.AddScoped<IAccountAuthService, AccountAuthService>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddControllers()
            .AddApplicationPart(typeof(UsersModuleMarker).Assembly);

        return services;
    }
}
