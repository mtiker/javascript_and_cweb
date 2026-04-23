using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class ResourceAuthorizationChecker(
    IAppDbContext dbContext,
    ICurrentActorResolver currentActorResolver) : IResourceAuthorizationChecker
{
    public async Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var context = currentActorResolver.GetCurrent();
        if (currentActorResolver.HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (!context.HasRole(RoleNames.Member))
        {
            throw new ForbiddenException("Only gym admins or the owning member can access this resource.");
        }

        var currentMember = await currentActorResolver.GetCurrentMemberAsync(gymId, cancellationToken);
        if (currentMember?.Id != memberId)
        {
            throw new ForbiddenException("Members can access only their own records.");
        }
    }

    public async Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var context = currentActorResolver.GetCurrent();
        if (currentActorResolver.HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (context.HasRole(RoleNames.Member))
        {
            await EnsureMemberSelfAccessAsync(booking.GymId, booking.MemberId, cancellationToken);
            return;
        }

        if (context.HasRole(RoleNames.Trainer))
        {
            var currentStaff = await currentActorResolver.GetCurrentStaffAsync(booking.GymId, cancellationToken);
            if (currentStaff == null)
            {
                throw new ForbiddenException("Trainer staff profile not found for the active gym.");
            }

            var assigned = await dbContext.WorkShifts.AnyAsync(shift =>
                    shift.GymId == booking.GymId &&
                    shift.TrainingSessionId == booking.TrainingSessionId &&
                    shift.Contract!.StaffId == currentStaff.Id &&
                    shift.ShiftType == ShiftType.Training,
                cancellationToken);

            if (!assigned)
            {
                throw new ForbiddenException("Trainers can access bookings only for sessions assigned to them.");
            }

            return;
        }

        throw new ForbiddenException("You do not have permission to access this booking.");
    }

    public async Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default)
    {
        var context = currentActorResolver.GetCurrent();
        if (currentActorResolver.HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (!context.HasRole(RoleNames.Trainer))
        {
            throw new ForbiddenException("Only assigned trainers or gym admins can update attendance.");
        }

        var currentStaff = await currentActorResolver.GetCurrentStaffAsync(trainingSession.GymId, cancellationToken);
        if (currentStaff == null)
        {
            throw new ForbiddenException("Trainer staff profile not found for the active gym.");
        }

        var assigned = await dbContext.WorkShifts.AnyAsync(shift =>
                shift.GymId == trainingSession.GymId &&
                shift.TrainingSessionId == trainingSession.Id &&
                shift.Contract!.StaffId == currentStaff.Id &&
                shift.ShiftType == ShiftType.Training,
            cancellationToken);

        if (!assigned)
        {
            throw new ForbiddenException("Only assigned trainers can update session attendance.");
        }
    }

    public async Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default)
    {
        var context = currentActorResolver.GetCurrent();
        if (currentActorResolver.HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (!context.HasRole(RoleNames.Caretaker))
        {
            throw new ForbiddenException("Only assigned caretakers or gym admins can manage maintenance tasks.");
        }

        var currentStaff = await currentActorResolver.GetCurrentStaffAsync(task.GymId, cancellationToken);
        if (currentStaff == null || task.AssignedStaffId != currentStaff.Id)
        {
            throw new ForbiddenException("Caretakers can update only tasks assigned to them.");
        }
    }
}
