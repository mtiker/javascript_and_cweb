using App.BLL.Services;
using App.BLL.Services.Admin;
using App.BLL.Services.Client;
using App.BLL.Mapping;
using WebApp.Areas.Admin.Services;
using WebApp.Areas.Client.Services;

namespace WebApp.Setup;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IWorkspaceContextService, WorkspaceContextService>();
        services.AddScoped<ICurrentActorResolver, CurrentActorResolver>();
        services.AddScoped<ITenantAccessChecker, TenantAccessChecker>();
        services.AddScoped<IResourceAuthorizationChecker, ResourceAuthorizationChecker>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<ISubscriptionTierLimitService, SubscriptionTierLimitService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthResponseMapper, AuthResponseMapper>();
        services.AddScoped<IMemberMapper, MemberMapper>();
        services.AddScoped<ITrainingMapper, TrainingMapper>();
        services.AddScoped<IMembershipFinanceMapper, MembershipFinanceMapper>();
        services.AddScoped<IMaintenanceMapper, MaintenanceMapper>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IPlatformService, PlatformService>();
        services.AddScoped<IMemberWorkflowService, MemberWorkflowService>();
        services.AddScoped<IMemberWorkspaceService, MemberWorkspaceService>();
        services.AddScoped<IMembershipPackageService, MembershipPackageService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IBookingPricingService, BookingPricingService>();
        services.AddScoped<IMembershipWorkflowService, MembershipWorkflowService>();
        services.AddScoped<ITrainingWorkflowService, TrainingWorkflowService>();
        services.AddScoped<IMaintenanceWorkflowService, MaintenanceWorkflowService>();
        services.AddScoped<IStaffWorkflowService, StaffWorkflowService>();
        services.AddScoped<IAdminOperationsQueryService, AdminOperationsQueryService>();
        services.AddScoped<IAdminSessionsQueryService, AdminSessionsQueryService>();
        services.AddScoped<IClientDashboardQueryService, ClientDashboardQueryService>();
        services.AddScoped<IClientSessionsQueryService, ClientSessionsQueryService>();
        services.AddScoped<IAdminDashboardPageService, AdminDashboardPageService>();
        services.AddScoped<IAdminGymsPageService, AdminGymsPageService>();
        services.AddScoped<IAdminOperationsPageService, AdminOperationsPageService>();
        services.AddScoped<IAdminSessionsPageService, AdminSessionsPageService>();
        services.AddScoped<IAdminMembersPageService, AdminMembersPageService>();
        services.AddScoped<IAdminMembershipPackagesPageService, AdminMembershipPackagesPageService>();
        services.AddScoped<IAdminTrainingCategoriesPageService, AdminTrainingCategoriesPageService>();
        services.AddScoped<IClientDashboardPageService, ClientDashboardPageService>();
        services.AddScoped<IClientSessionsPageService, ClientSessionsPageService>();
        services.AddScoped<IClientProfilePageService, ClientProfilePageService>();
        services.AddScoped<IClientMaintenancePageService, ClientMaintenancePageService>();

        return services;
    }
}
