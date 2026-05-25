using App.BLL.Contracts.Infrastructure;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Modules.Memberships.Application.Persistence;
using Shared.Contracts.Enums;
using Shared.Contracts.Mediator.Diagnostics;
using Shared.Contracts.Mediator.Notifications;

namespace Modules.Memberships.Application.Mediator;

/// <summary>
/// Subscribes to <see cref="BookingConfirmedNotification"/> published by the
/// Training module. Increments <see cref="Membership.SessionsConsumed"/> on the
/// member's active membership in the same gym so the Memberships module owns
/// its own consumption state without exposing it back through a direct call.
/// </summary>
internal sealed class BookingConfirmedHandler(
    IMembershipRepository membershipRepository,
    IAppDbContext dbContext,
    IModuleEventRecorder eventRecorder,
    ILogger<BookingConfirmedHandler> logger) : INotificationHandler<BookingConfirmedNotification>
{
    public async Task Handle(BookingConfirmedNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var memberships = await membershipRepository.ListForMemberAsync(
            notification.GymId,
            notification.MemberId,
            cancellationToken);

        var activeMembership = memberships.FirstOrDefault(m => m.Status == MembershipStatus.Active);
        if (activeMembership is null)
        {
            logger.LogInformation(
                "BookingConfirmedNotification ignored: member {MemberId} in gym {GymId} has no active membership.",
                notification.MemberId,
                notification.GymId);
            eventRecorder.Record($"Modules.Memberships<-BookingConfirmed:no-active:{notification.BookingId}");
            return;
        }

        activeMembership.SessionsConsumed += 1;
        await dbContext.SaveChangesAsync(cancellationToken);

        eventRecorder.Record($"Modules.Memberships<-BookingConfirmed:{notification.BookingId}");
    }
}
