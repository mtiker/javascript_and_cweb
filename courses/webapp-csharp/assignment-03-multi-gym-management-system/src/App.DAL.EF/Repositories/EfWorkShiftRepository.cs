using App.BLL.Contracts.Persistence;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfWorkShiftRepository(AppDbContext dbContext) : IWorkShiftRepository
{
    public async Task<IReadOnlyList<WorkShift>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkShifts
            .Where(workShift => workShift.GymId == gymId)
            .OrderBy(workShift => workShift.StartAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkShift>> ListForStaffAsync(Guid gymId, Guid staffId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkShifts
            .Include(workShift => workShift.Contract)
            .Where(workShift => workShift.GymId == gymId && workShift.Contract != null && workShift.Contract.StaffId == staffId)
            .OrderBy(workShift => workShift.StartAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkShift>> ListTrainingShiftsForSessionAsync(Guid gymId, Guid trainingSessionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkShifts
            .Where(workShift =>
                workShift.GymId == gymId &&
                workShift.TrainingSessionId == trainingSessionId &&
                workShift.ShiftType == ShiftType.Training)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkShift>> ListTrainingShiftsWithStaffForSessionAsync(Guid gymId, Guid trainingSessionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkShifts
            .AsNoTracking()
            .Include(workShift => workShift.Contract)
                .ThenInclude(contract => contract!.Staff)
                    .ThenInclude(staff => staff!.Person)
            .Where(workShift =>
                workShift.GymId == gymId &&
                workShift.TrainingSessionId == trainingSessionId &&
                workShift.ShiftType == ShiftType.Training)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListTrainerContractIdsForSessionAsync(Guid gymId, Guid trainingSessionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkShifts
            .Where(workShift =>
                workShift.GymId == gymId &&
                workShift.TrainingSessionId == trainingSessionId &&
                workShift.ShiftType == ShiftType.Training)
            .Select(workShift => workShift.ContractId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsTrainingShiftForStaffAsync(Guid gymId, Guid trainingSessionId, Guid staffId, CancellationToken cancellationToken = default)
    {
        return dbContext.WorkShifts.AnyAsync(
            workShift =>
                workShift.GymId == gymId &&
                workShift.TrainingSessionId == trainingSessionId &&
                workShift.ShiftType == ShiftType.Training &&
                workShift.Contract != null &&
                workShift.Contract.StaffId == staffId,
            cancellationToken);
    }

    public Task<WorkShift?> FindAsync(Guid gymId, Guid workShiftId, CancellationToken cancellationToken = default)
    {
        return dbContext.WorkShifts
            .FirstOrDefaultAsync(workShift => workShift.GymId == gymId && workShift.Id == workShiftId, cancellationToken);
    }

    public async Task AddAsync(WorkShift workShift, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workShift);
        await dbContext.WorkShifts.AddAsync(workShift, cancellationToken);
    }

    public void Remove(WorkShift workShift)
    {
        ArgumentNullException.ThrowIfNull(workShift);
        dbContext.WorkShifts.Remove(workShift);
    }

    public void RemoveRange(IEnumerable<WorkShift> workShifts)
    {
        ArgumentNullException.ThrowIfNull(workShifts);
        dbContext.WorkShifts.RemoveRange(workShifts);
    }
}
