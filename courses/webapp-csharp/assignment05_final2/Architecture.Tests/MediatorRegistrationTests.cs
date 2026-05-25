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
}
