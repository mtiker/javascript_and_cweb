using App.BLL.Contracts.Services;
using App.BLL.Contracts.Services.Admin;
using App.BLL.Contracts.Services.Client;
using WebApp.Areas.Admin.Queries;
using WebApp.Areas.Admin.Services;
using WebApp.Areas.Client.Queries;
using WebApp.Areas.Client.Services;

namespace WebApp.Setup;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Domain application services are registered by their owning modules.
        // This method keeps WebApp-owned MVC page and query services only.
        services.AddScoped<IAdminOperationsQueryService, AdminOperationsQueryService>();
        services.AddScoped<IAdminSessionsQueryService, AdminSessionsQueryService>();
        services.AddScoped<IClientDashboardQueryService, ClientDashboardQueryService>();
        services.AddScoped<IClientSessionsQueryService, ClientSessionsQueryService>();
        services.AddScoped<IAdminDashboardPageService, AdminDashboardPageService>();
        services.AddScoped<IAdminGymsPageService, AdminGymsPageService>();
        services.AddScoped<IAdminOperationsPageService, AdminOperationsPageService>();
        services.AddScoped<IAdminSessionsPageService, AdminSessionsPageService>();
        services.AddScoped<IAdminMembersPageService, AdminMembersPageService>();
        services.AddScoped<IAdminMembershipsPageService, AdminMembershipsPageService>();
        services.AddScoped<IAdminMembershipPackagesPageService, AdminMembershipPackagesPageService>();
        services.AddScoped<IAdminTrainingCategoriesPageService, AdminTrainingCategoriesPageService>();
        services.AddScoped<IClientDashboardPageService, ClientDashboardPageService>();
        services.AddScoped<IClientSessionsPageService, ClientSessionsPageService>();
        services.AddScoped<IClientProfilePageService, ClientProfilePageService>();
        services.AddScoped<IClientMaintenancePageService, ClientMaintenancePageService>();

        return services;
    }
}
