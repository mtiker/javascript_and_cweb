using BuildingBlocks.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Training;

/// <summary>
/// DI registration entry point for the Training module. Phase 16 only
/// registers the mediator handler scan; member, training-category,
/// training-session, booking, and coaching-plan services move into this
/// module in Phase 18.
/// </summary>
public static class TrainingModuleServiceCollectionExtensions
{
    public static IServiceCollection AddTrainingModule(this IServiceCollection services)
    {
        services.AddModuleMediatorHandlersFromAssembly(typeof(TrainingModuleServiceCollectionExtensions).Assembly);
        return services;
    }
}
