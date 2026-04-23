using App.BLL.Services;

namespace WebApp.Setup;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<ICurrentActorResolver, CurrentActorResolver>();
        services.AddScoped<ITenantAccessChecker, TenantAccessChecker>();
        services.AddScoped<IResourceAuthorizationChecker, ResourceAuthorizationChecker>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<ISubscriptionTierLimitService, SubscriptionTierLimitService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IPlatformService, PlatformService>();
        services.AddScoped<IMemberWorkflowService, MemberWorkflowService>();
        services.AddScoped<IMemberWorkspaceService, MemberWorkspaceService>();
        services.AddScoped<IMembershipPackageService, MembershipPackageService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IBookingPricingService, BookingPricingService>();
        services.AddScoped<IMembershipWorkflowService, MembershipWorkflowService>();
        services.AddScoped<ICoachingPlanService, CoachingPlanService>();
        services.AddScoped<IFinanceWorkspaceService, FinanceWorkspaceService>();
        services.AddScoped<ITrainingWorkflowService, TrainingWorkflowService>();
        services.AddScoped<IMaintenanceWorkflowService, MaintenanceWorkflowService>();
        services.AddScoped<IStaffWorkflowService, StaffWorkflowService>();

        return services;
    }
}
