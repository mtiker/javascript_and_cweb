using App.Domain.Entities;

namespace App.BLL.Contracts.Persistence;

public interface IWorkShiftRepository
{
    Task<IReadOnlyList<WorkShift>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkShift>> ListForStaffAsync(Guid gymId, Guid staffId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkShift>> ListTrainingShiftsForSessionAsync(Guid gymId, Guid trainingSessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkShift>> ListTrainingShiftsWithStaffForSessionAsync(Guid gymId, Guid trainingSessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListTrainerContractIdsForSessionAsync(Guid gymId, Guid trainingSessionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsTrainingShiftForStaffAsync(Guid gymId, Guid trainingSessionId, Guid staffId, CancellationToken cancellationToken = default);

    Task<WorkShift?> FindAsync(Guid gymId, Guid workShiftId, CancellationToken cancellationToken = default);

    Task AddAsync(WorkShift workShift, CancellationToken cancellationToken = default);

    void Remove(WorkShift workShift);

    void RemoveRange(IEnumerable<WorkShift> workShifts);
}
