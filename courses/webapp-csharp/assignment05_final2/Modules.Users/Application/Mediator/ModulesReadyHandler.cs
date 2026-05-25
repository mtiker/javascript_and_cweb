using MediatR;
using Shared.Contracts.Mediator.Diagnostics;
using Shared.Contracts.Mediator.Events;

namespace Modules.Users.Application.Mediator;

/// <summary>
/// Phase 3 proof-of-life: the Users module subscribes to the cross-module
/// <see cref="ModulesReadyNotification"/> defined in <c>Shared.Contracts</c>.
/// Successful dispatch demonstrates that <c>AddUsersModule</c> registers
/// handlers from this assembly via MediatR.
/// </summary>
internal sealed class ModulesReadyHandler : INotificationHandler<ModulesReadyNotification>
{
    private readonly IModuleEventRecorder _recorder;

    public ModulesReadyHandler(IModuleEventRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task Handle(ModulesReadyNotification notification, CancellationToken cancellationToken)
    {
        _recorder.Record($"Modules.Users<-{notification.Source}");
        return Task.CompletedTask;
    }
}
