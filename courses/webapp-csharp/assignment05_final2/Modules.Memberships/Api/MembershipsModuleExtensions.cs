using SharedKernel.Persistence;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Modules.Memberships.Application;
using Modules.Memberships.Application.Mappers;
using Modules.Memberships.Application.Persistence;
using Modules.Memberships.Infrastructure;
using Modules.Memberships.Infrastructure.Persistence;
using App.BLL.Contracts.Services;
using Shared.Contracts.Mediator.Diagnostics;
using Shared.Contracts.ModuleApis;

namespace Modules.Memberships.Api;

public static class MembershipsModuleExtensions
{
    /// <summary>
    /// Registers the Memberships module: MediatR handler discovery, the module's
    /// outward <see cref="IMembershipsModuleApi"/> contract (Phase 6), and the
    /// module assembly as an MVC <see cref="ApplicationPart"/> so the relocated
    /// tenant controllers (members, memberships, membership-packages, payments,
    /// member-workspace) are discovered when WebApp scans for controllers. The
    /// workflow/workspace services now live in this module's Application
    /// layer and are registered here.
    /// </summary>
    public static IServiceCollection AddMembershipsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MembershipsModuleMarker).Assembly));
        services.TryAddSingleton<IModuleEventRecorder, InMemoryModuleEventRecorder>();

        services.AddModuleDbContext<MembershipsDbContext>(configuration);
        services.AddScoped<IMemberRepository, EfMemberRepository>();
        services.AddScoped<IMembershipPackageRepository, EfMembershipPackageRepository>();
        services.AddScoped<IMembershipRepository, EfMembershipRepository>();
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IMembershipsModuleApi, MembershipsModuleApiService>();
        services.AddScoped<IMemberMapper, MemberMapper>();
        services.AddScoped<IMembershipFinanceMapper, MembershipFinanceMapper>();
        services.AddScoped<IMemberWorkflowService, MemberWorkflowService>();
        services.AddScoped<IMemberWorkspaceService, MemberWorkspaceService>();
        services.AddScoped<IMembershipPackageService, MembershipPackageService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IMembershipWorkflowService, MembershipWorkflowService>();

        services.AddControllers()
            .AddApplicationPart(typeof(MembershipsModuleMarker).Assembly);

        return services;
    }
}
