namespace App.BLL.Services.Client;

public interface IClientSessionsQueryService
{
    Task<ClientSessionDetailSnapshot> GetDetailSnapshotAsync(
        Guid gymId,
        Guid sessionId,
        Guid? currentMemberId,
        Guid? currentStaffId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientSessionRosterRow>> GetRosterBookingsAsync(
        Guid gymId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<bool> HasTrainerAssignmentAsync(
        Guid gymId,
        Guid sessionId,
        Guid staffId,
        CancellationToken cancellationToken = default);
}
