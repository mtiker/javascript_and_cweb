using BuildingBlocks.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.GymManagement;

/// <summary>
/// DI registration entry point for the GymManagement module. Phase 16 only
/// registers the mediator handler scan; staff/maintenance/equipment services
/// move into this module in Phase 20.
/// </summary>
public static class GymManagementModuleServiceCollectionExtensions
{
    public static IServiceCollection AddGymManagementModule(this IServiceCollection services)
    {
        services.AddModuleMediatorHandlersFromAssembly(typeof(GymManagementModuleServiceCollectionExtensions).Assembly);
        return services;
    }
}
