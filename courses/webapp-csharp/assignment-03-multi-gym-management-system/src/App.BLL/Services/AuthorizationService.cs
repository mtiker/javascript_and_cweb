using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.Domain.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace App.BLL.Services;

public class AuthorizationService(
    IAppDbContext dbContext,
    IUserContextService userContextService) : IAuthorizationService
{
    public async Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles)
    {
        var context = userContextService.GetCurrent();
        if (!context.IsAuthenticated || !context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            throw new ForbiddenException("An active gym context is required.");
        }

        var gym = await dbContext.Gyms.AsNoTracking().FirstOrDefaultAsync(entity => entity.Code == gymCode);
        if (gym == null)
        {
            throw new NotFoundException($"Gym '{gymCode}' was not found.");
        }

        if (context.ActiveGymId != gym.Id || !string.Equals(context.ActiveGymCode, gymCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("The requested gym does not match the active gym context.");
        }

        if (allowedRoles.Length > 0 && !allowedRoles.Any(context.HasRole))
        {
            throw new ForbiddenException("You do not have permission to access this gym resource.");
        }

        return gym.Id;
    }

    public async Task<Member?> GetCurrentMemberAsync(Guid gymId)
    {
        var context = userContextService.GetCurrent();
        if (!context.PersonId.HasValue)
        {
            return null;
        }

        return await dbContext.Members.FirstOrDefaultAsync(member => member.GymId == gymId && member.PersonId == context.PersonId);
    }

    public async Task<Staff?> GetCurrentStaffAsync(Guid gymId)
    {
        var context = userContextService.GetCurrent();
        if (!context.PersonId.HasValue)
        {
            return null;
        }

        return await dbContext.Staff.FirstOrDefaultAsync(staff => staff.GymId == gymId && staff.PersonId == context.PersonId);
    }

    public async Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId)
    {
        var context = userContextService.GetCurrent();
        if (HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (!context.HasRole(RoleNames.Member))
        {
            throw new ForbiddenException("Only gym admins or the owning member can access this resource.");
        }

        var currentMember = await GetCurrentMemberAsync(gymId);
        if (currentMember?.Id != memberId)
        {
            throw new ForbiddenException("Members can access only their own records.");
        }
    }

    public async Task EnsureBookingAccessAsync(Booking booking)
    {
        var context = userContextService.GetCurrent();
        if (HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (context.HasRole(RoleNames.Member))
        {
            await EnsureMemberSelfAccessAsync(booking.GymId, booking.MemberId);
            return;
        }

        if (context.HasRole(RoleNames.Trainer))
        {
            var currentStaff = await GetCurrentStaffAsync(booking.GymId);
            if (currentStaff == null)
            {
                throw new ForbiddenException("Trainer staff profile not found for the active gym.");
            }

            var assigned = await dbContext.WorkShifts.AnyAsync(shift =>
                shift.GymId == booking.GymId &&
                shift.TrainingSessionId == booking.TrainingSessionId &&
                shift.Contract!.StaffId == currentStaff.Id &&
                shift.ShiftType == ShiftType.Training);

            if (!assigned)
            {
                throw new ForbiddenException("Trainers can access bookings only for sessions assigned to them.");
            }

            return;
        }

        throw new ForbiddenException("You do not have permission to access this booking.");
    }

    public async Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession)
    {
        var context = userContextService.GetCurrent();
        if (HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (!context.HasRole(RoleNames.Trainer))
        {
            throw new ForbiddenException("Only assigned trainers or gym admins can update attendance.");
        }

        var currentStaff = await GetCurrentStaffAsync(trainingSession.GymId);
        if (currentStaff == null)
        {
            throw new ForbiddenException("Trainer staff profile not found for the active gym.");
        }

        var assigned = await dbContext.WorkShifts.AnyAsync(shift =>
            shift.GymId == trainingSession.GymId &&
            shift.TrainingSessionId == trainingSession.Id &&
            shift.Contract!.StaffId == currentStaff.Id &&
            shift.ShiftType == ShiftType.Training);

        if (!assigned)
        {
            throw new ForbiddenException("Only assigned trainers can update session attendance.");
        }
    }

    public async Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task)
    {
        var context = userContextService.GetCurrent();
        if (HasTenantAdminPrivileges(context))
        {
            return;
        }

        if (!context.HasRole(RoleNames.Caretaker))
        {
            throw new ForbiddenException("Only assigned caretakers or gym admins can manage maintenance tasks.");
        }

        var currentStaff = await GetCurrentStaffAsync(task.GymId);
        if (currentStaff == null || task.AssignedStaffId != currentStaff.Id)
        {
            throw new ForbiddenException("Caretakers can update only tasks assigned to them.");
        }
    }

    private static bool HasTenantAdminPrivileges(UserExecutionContext context)
    {
        return context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin);
    }
}
