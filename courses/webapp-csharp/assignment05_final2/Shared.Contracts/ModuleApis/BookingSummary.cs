namespace Shared.Contracts.ModuleApis;

/// <summary>
/// Public projection of a booking owned by the Training module. Cross-module
/// consumers use this instead of importing Training repositories or entities.
/// </summary>
public sealed record BookingSummary(
    Guid Id,
    Guid GymId,
    Guid MemberId,
    Guid TrainingSessionId);
