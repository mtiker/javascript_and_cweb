using App.BLL.Contracts.Services;
using SharedKernel.Persistence;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Modules.Training.Application;
using Modules.Training.Application.Mappers;
using Modules.Training.Application.Persistence;
using Modules.Training.Application.Pricing;
using Modules.Training.Infrastructure;
using Modules.Training.Infrastructure.Persistence;
using Shared.Contracts.Mediator.Diagnostics;
using Shared.Contracts.ModuleApis;

namespace Modules.Training.Api;

public static class TrainingModuleExtensions
{
    /// <summary>
    /// Registers the Training module: MediatR handler discovery, module-owned
    /// training repositories/workflow services (Phase 7), the outward
    /// <see cref="ITrainingModuleApi"/> contract, and the module assembly as
    /// an MVC <see cref="ApplicationPart"/> so the relocated staff, training,
    /// and booking controllers are discovered by WebApp.
    /// </summary>
    public static IServiceCollection AddTrainingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(TrainingModuleMarker).Assembly));
        services.TryAddSingleton<IModuleEventRecorder, InMemoryModuleEventRecorder>();

        services.AddModuleDbContext<TrainingDbContext>(configuration);
        services.AddScoped<ITrainingCategoryRepository, EfTrainingCategoryRepository>();
        services.AddScoped<ITrainingSessionRepository, EfTrainingSessionRepository>();
        services.AddScoped<IBookingRepository, EfBookingRepository>();
        services.AddScoped<ITrainingPersistenceContext, EfTrainingPersistenceContext>();

        services.AddScoped<ITrainingMapper, TrainingMapper>();
        services.AddScoped<IBookingPricingService, BookingPricingService>();
        services.AddScoped<ITrainingWorkflowService, TrainingWorkflowService>();
        services.AddScoped<IStaffWorkflowService, StaffWorkflowService>();
        services.AddScoped<ITrainingModuleApi, TrainingModuleApiService>();

        services.AddControllers()
            .AddApplicationPart(typeof(TrainingModuleMarker).Assembly);

        return services;
    }
}
