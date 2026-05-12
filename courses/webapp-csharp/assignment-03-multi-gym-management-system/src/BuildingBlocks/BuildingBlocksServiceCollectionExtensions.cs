using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks;

/// <summary>
/// Composition-root entry point for shared abstractions. Modules do not call
/// this themselves; the WebApp host calls it once.
/// </summary>
public static class BuildingBlocksServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        services.AddScoped<BuildingBlocks.Mediator.IMediator, BuildingBlocks.Mediator.Mediator>();
        return services;
    }
}
