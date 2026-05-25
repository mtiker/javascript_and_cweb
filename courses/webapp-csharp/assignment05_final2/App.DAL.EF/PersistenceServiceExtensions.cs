using App.DAL.Contracts.Persistence;
using App.DAL.EF.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace App.DAL.EF;

public static class PersistenceServiceExtensions
{
    public static IServiceCollection AddAppPersistence(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        // Gym authorization lookup moved to Modules.Gyms (Phase 10d).
        // Training repositories moved to Modules.Training and are no longer
        // exposed through the transitional unit of work (Phase 10d).
        // Membership repositories moved to Modules.Memberships and are no
        // longer exposed through the transitional unit of work (Phase 10d).
        // Maintenance repository moved to Modules.Maintenance (Phase 8).
        services.AddScoped<IAppUnitOfWork, AppUOW>();
        return services;
    }
}
