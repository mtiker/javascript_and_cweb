using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Gyms.Api;
using Modules.Maintenance.Api;
using Modules.Memberships.Api;
using Modules.Training.Api;
using Modules.Users.Api;
using Shared.Contracts.Mediator.Diagnostics;
using Shared.Contracts.Mediator.Events;
using Shared.Contracts.Mediator.Notifications;

namespace Architecture.Tests;

/// <summary>
/// Phase 3 proof-of-life: composing every <c>AddXxxModule</c> against a bare
/// <see cref="ServiceCollection"/> must yield a resolvable
/// <see cref="IMediator"/> and the Users module's handler must observe a
/// published <see cref="ModulesReadyNotification"/>.
/// </summary>
[Trait("Category", "Architecture")]
public class MediatorRegistrationTests
{
    [Fact]
    public async Task EachModuleRegistration_AllowsResolvingMediator_AndUsersHandlerObservesSampleEvent()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddUsersModule(configuration);
        services.AddGymsModule(configuration);
        services.AddMembershipsModule(configuration);
        services.AddTrainingModule(configuration);
        services.AddMaintenanceModule(configuration);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var recorder = scope.ServiceProvider.GetRequiredService<IModuleEventRecorder>();

        await mediator.Publish(new ModulesReadyNotification("Architecture.Tests"));

        Assert.Contains(recorder.Snapshot(), entry => entry == "Modules.Users<-Architecture.Tests");
    }

    [Fact]
    public void ModuleEventRecorder_IsRegisteredAsSingleton_AcrossModuleAdditions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddUsersModule(configuration);
        services.AddGymsModule(configuration);
        services.AddMembershipsModule(configuration);
        services.AddTrainingModule(configuration);
        services.AddMaintenanceModule(configuration);

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IModuleEventRecorder>();
        var second = provider.GetRequiredService<IModuleEventRecorder>();

        Assert.Same(first, second);
    }

    [Fact]
    public void MembershipsModule_RegistersHandler_ForBookingConfirmedNotification_FromTraining()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddMembershipsModule(configuration);

        var handlerInterface = typeof(INotificationHandler<BookingConfirmedNotification>);
        var descriptor = services.FirstOrDefault(d => d.ServiceType == handlerInterface);

        Assert.NotNull(descriptor);
        var implementationType = descriptor!.ImplementationType
            ?? descriptor.ImplementationInstance?.GetType()
            ?? descriptor.ImplementationFactory?.Method.ReturnType;
        Assert.NotNull(implementationType);
        Assert.Equal("Modules.Memberships", implementationType!.Assembly.GetName().Name);
        Assert.Equal("BookingConfirmedHandler", implementationType.Name);

        // Strict module-boundary check: Training (the publisher) MUST NOT
        // register the handler. The handler lives in Memberships precisely so
        // Training can publish a notification without naming the consumer.
        var trainingServices = new ServiceCollection();
        trainingServices.AddTrainingModule(configuration);
        Assert.DoesNotContain(
            trainingServices,
            d => d.ServiceType == handlerInterface);
    }
}
