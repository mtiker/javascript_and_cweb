using App.BLL.Contracts.Infrastructure;
using App.BLL.Contracts.Services;
using SharedKernel.Exceptions;
using SharedKernel;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace Modules.Gyms.Application.Authorization;

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
        await EnsureBookingAccessAsync(
            booking.GymId,
            booking.MemberId,
            booking.TrainingSessionId,
            cancellationToken);
    }

    public async Task EnsureBookingAccessAsync(Guid gymId, Guid memberId, Guid trainingSessionId, CancellationToken cancellationToken = default)
    {
        var context = currentActorResolver.GetCurrent();
        if (currentActorResolver.HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (context.HasRole(RoleNames.Member))
        {
            await EnsureMemberSelfAccessAsync(gymId, memberId, cancellationToken);
            return;
        }

        if (context.HasRole(RoleNames.Trainer))
        {
            var currentStaff = await currentActorResolver.GetCurrentStaffAsync(gymId, cancellationToken);
            if (currentStaff == null)
            {
                throw new ForbiddenException("Trainer staff profile not found for the active gym.");
            }

            var assigned = await dbContext.TrainingSessions.AnyAsync(session =>
                    session.GymId == gymId &&
                    session.Id == trainingSessionId &&
                    session.TrainerStaffId == currentStaff.Id,
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

        var assigned = trainingSession.TrainerStaffId == currentStaff.Id ||
                       await dbContext.TrainingSessions.AnyAsync(session =>
                               session.GymId == trainingSession.GymId &&
                               session.Id == trainingSession.Id &&
                               session.TrainerStaffId == currentStaff.Id,
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
