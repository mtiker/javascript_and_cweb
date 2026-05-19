using System.Globalization;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.DTO.v1.Staff;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class StaffWorkflowService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService,
    ISubscriptionTierLimitService subscriptionTierLimitService) : IStaffWorkflowService
{
    public async Task<IReadOnlyCollection<StaffResponse>> GetStaffAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode, cancellationToken);
        return await dbContext.Staff
            .Include(entity => entity.Person)
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.Person!.LastName)
            .ThenBy(entity => entity.Person!.FirstName)
            .Select(entity => new StaffResponse
            {
                Id = entity.Id,
                StaffCode = entity.StaffCode,
                FullName = $"{entity.Person!.FirstName} {entity.Person!.LastName}".Trim(),
                Status = entity.Status
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<StaffResponse> CreateStaffAsync(string gymCode, StaffUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode, cancellationToken);
        await subscriptionTierLimitService.EnsureCanCreateStaffAsync(gymId, cancellationToken);
        var staff = new Staff
        {
            GymId = gymId,
            StaffCode = request.StaffCode.Trim(),
            Status = request.Status,
            Person = new Person
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim()
            }
        };

        dbContext.Staff.Add(staff);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToStaffResponse(staff);
    }

    public async Task<StaffResponse> UpdateStaffAsync(string gymCode, Guid id, StaffUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode, cancellationToken);
        var staff = await dbContext.Staff
                        .Include(entity => entity.Person)
                        .FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId, cancellationToken)
                    ?? throw new NotFoundException("Staff member was not found.");

        staff.StaffCode = request.StaffCode.Trim();
        staff.Status = request.Status;
        staff.Person!.FirstName = request.FirstName.Trim();
        staff.Person.LastName = request.LastName.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToStaffResponse(staff);
    }

    public async Task DeleteStaffAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await EnsureStaffAdminAccessAsync(gymCode, cancellationToken);
        var staff = await dbContext.Staff.FirstOrDefaultAsync(entity => entity.Id == id && entity.GymId == gymId, cancellationToken)
                    ?? throw new NotFoundException("Staff member was not found.");

        dbContext.Staff.Remove(staff);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<Guid> EnsureStaffAdminAccessAsync(string gymCode, CancellationToken cancellationToken)
    {
        return authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
    }

    private static StaffResponse ToStaffResponse(Staff staff)
    {
        return new StaffResponse
        {
            Id = staff.Id,
            StaffCode = staff.StaffCode,
            FullName = $"{staff.Person!.FirstName} {staff.Person.LastName}".Trim(),
            Status = staff.Status
        };
    }

}
