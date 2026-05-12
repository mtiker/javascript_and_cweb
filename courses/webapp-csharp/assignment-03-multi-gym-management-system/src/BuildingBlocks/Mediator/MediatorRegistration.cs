using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Mediator;

/// <summary>
/// Module DI helpers for registering mediator handlers. Each module calls
/// <see cref="AddModuleMediatorHandlersFromAssembly"/> from its own DI extension
/// and passes its assembly so handlers from other modules are not registered.
/// </summary>
public static class MediatorRegistration
{
    private static readonly Type[] OpenHandlerInterfaces =
    {
        typeof(IRequestHandler<>),
        typeof(IRequestHandler<,>),
    };

    public static IServiceCollection AddModuleMediatorHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
            {
                continue;
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                {
                    continue;
                }

                var def = iface.GetGenericTypeDefinition();
                if (Array.IndexOf(OpenHandlerInterfaces, def) < 0)
                {
                    continue;
                }

                services.AddScoped(iface, type);
            }
        }

        return services;
    }
}
