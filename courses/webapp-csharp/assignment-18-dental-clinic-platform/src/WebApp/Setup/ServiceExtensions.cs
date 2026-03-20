using App.BLL.Services;
using App.DAL.EF.Tenant;
using WebApp.Helpers;

namespace WebApp.Setup;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlagStore, FeatureFlagStore>();
        services.AddScoped<ITenantProvider, RequestTenantProvider>();
        services.AddScoped<ICompanyOnboardingService, CompanyOnboardingService>();
        services.AddScoped<ICompanySettingsService, CompanySettingsService>();
        services.AddScoped<ICompanyUserService, CompanyUserService>();
        services.AddScoped<ISubscriptionPolicyService, SubscriptionPolicyService>();
        services.AddScoped<ITenantAccessService, TenantAccessService>();
        services.AddScoped<ITreatmentPlanService, TreatmentPlanService>();
        services.AddScoped<IFinanceWorkspaceService, FinanceWorkspaceService>();
        services.AddScoped<ICostEstimateService, CostEstimateService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentPlanService, PaymentPlanService>();
        services.AddScoped<IImpersonationService, ImpersonationService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
