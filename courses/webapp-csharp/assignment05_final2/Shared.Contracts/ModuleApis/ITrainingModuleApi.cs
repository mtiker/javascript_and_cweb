namespace Shared.Contracts.ModuleApis;

/// <summary>
/// The Training module's outward-facing contract. Other modules should use
/// this projection API when they need staff/session/booking identity checks
/// without referencing Training EF/domain entities directly.
/// </summary>
public interface ITrainingModuleApi
{
    Task<StaffSummary?> GetStaffSummaryAsync(
        Guid gymId,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<TrainingSessionSummary?> GetTrainingSessionSummaryAsync(
        Guid gymId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<BookingSummary?> GetBookingSummaryAsync(
        Guid gymId,
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListBookingIdsForMemberAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken = default);
}
