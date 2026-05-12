using BuildingBlocks;
using Modules.GymManagement;
using Modules.MembershipFinance;
using Modules.Training;
using Modules.Users;

namespace WebApp.Setup;

/// <summary>
/// Composition-root entry point for Final-2 modules. Each module exposes a
/// single <c>Add&lt;Name&gt;Module</c> extension; this file is the only place
/// that knows the full module set.
/// </summary>
public static class ModuleExtensions
{
    public static IServiceCollection AddAppModules(this IServiceCollection services)
    {
        services.AddBuildingBlocks();
        services.AddUsersModule();
        services.AddGymManagementModule();
        services.AddTrainingModule();
        services.AddMembershipFinanceModule();
        return services;
    }
}
