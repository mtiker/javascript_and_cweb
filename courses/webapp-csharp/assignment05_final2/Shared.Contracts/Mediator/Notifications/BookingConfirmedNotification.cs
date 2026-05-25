namespace Shared.Contracts.Mediator.Notifications;

/// <summary>
/// Published by the Training module after a booking row is confirmed and
/// persisted. Subscribed to by the Memberships module to update its own
/// consumption counters without any direct project reference from Training
/// into Memberships. Carries only scalar identifiers so it crosses the module
/// seam through <see cref="Shared.Contracts"/> alone.
/// </summary>
public sealed record BookingConfirmedNotification(
    Guid GymId,
    Guid BookingId,
    Guid MemberId,
    Guid TrainingSessionId,
    DateTimeOffset ConfirmedAtUtc) : IModuleNotification;
