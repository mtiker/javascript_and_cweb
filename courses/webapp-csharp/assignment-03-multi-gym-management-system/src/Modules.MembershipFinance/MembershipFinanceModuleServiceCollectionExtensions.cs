using BuildingBlocks.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.MembershipFinance;

/// <summary>
/// DI registration entry point for the MembershipFinance module. Phase 16 only
/// registers the mediator handler scan; package, membership, payment, and
/// finance-workspace services move into this module in Phase 19.
/// </summary>
public static class MembershipFinanceModuleServiceCollectionExtensions
{
    public static IServiceCollection AddMembershipFinanceModule(this IServiceCollection services)
    {
        services.AddModuleMediatorHandlersFromAssembly(typeof(MembershipFinanceModuleServiceCollectionExtensions).Assembly);
        return services;
    }
}
