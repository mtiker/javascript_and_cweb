using App.BLL.Contracts.Persistence;
using App.DAL.EF.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace App.DAL.EF;

public static class PersistenceServiceExtensions
{
    public static IServiceCollection AddAppPersistence(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddScoped<IMemberRepository, EfMemberRepository>();
        services.AddScoped<ITrainingCategoryRepository, EfTrainingCategoryRepository>();
        services.AddScoped<ITrainingSessionRepository, EfTrainingSessionRepository>();
        services.AddScoped<IBookingRepository, EfBookingRepository>();
        services.AddScoped<IWorkShiftRepository, EfWorkShiftRepository>();
        services.AddScoped<IMembershipPackageRepository, EfMembershipPackageRepository>();
        services.AddScoped<IMembershipRepository, EfMembershipRepository>();
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IFinanceRepository, EfFinanceRepository>();
        services.AddScoped<IMaintenanceRepository, EfMaintenanceRepository>();
        services.AddScoped<IAppUnitOfWork, EfAppUnitOfWork>();
        return services;
    }
}
