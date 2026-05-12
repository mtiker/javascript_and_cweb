using BuildingBlocks.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Modules.Users.Application.Auth;

namespace Modules.Users;

/// <summary>
/// DI registration entry point for the Users module.
/// </summary>
public static class UsersModuleServiceCollectionExtensions
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddScoped<IUsersSessionService, UsersSessionService>();
        services.AddModuleMediatorHandlersFromAssembly(typeof(UsersModuleServiceCollectionExtensions).Assembly);
        return services;
    }
}
