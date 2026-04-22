using App.BLL.Services;

namespace WebApp.Setup;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IPlatformService, PlatformService>();
        services.AddScoped<IMemberWorkflowService, MemberWorkflowService>();
        services.AddScoped<IMembershipWorkflowService, MembershipWorkflowService>();
        services.AddScoped<ITrainingWorkflowService, TrainingWorkflowService>();
        services.AddScoped<IMaintenanceWorkflowService, MaintenanceWorkflowService>();
        services.AddScoped<IStaffWorkflowService, StaffWorkflowService>();

        return services;
    }
}
