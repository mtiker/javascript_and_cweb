using App.BLL.Contracts;
using App.BLL.Exceptions;
using App.Domain.Entities;
using App.DTO.v1.Tenant;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class MemberWorkflowService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService) : IMemberWorkflowService
{
    public async Task<IReadOnlyCollection<MemberResponse>> GetMembersAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        return await dbContext.Members
            .Include(entity => entity.Person)
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.Person!.LastName)
            .ThenBy(entity => entity.Person!.FirstName)
            .Select(entity => new MemberResponse
            {
                Id = entity.Id,
                MemberCode = entity.MemberCode,
                FullName = $"{entity.Person!.FirstName} {entity.Person!.LastName}".Trim(),
                Status = entity.Status
            })
            .ToArrayAsync();
    }

    public async Task<MemberDetailResponse> GetCurrentMemberAsync(string gymCode)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            App.Domain.RoleNames.GymOwner,
            App.Domain.RoleNames.GymAdmin,
            App.Domain.RoleNames.Member);

        var currentMember = await authorizationService.GetCurrentMemberAsync(gymId)
                            ?? throw new AppNotFoundException("Current user does not have a member profile in the active gym.");

        var member = await dbContext.Members
            .Include(entity => entity.Person)
            .FirstOrDefaultAsync(entity => entity.Id == currentMember.Id)
            ?? throw new AppNotFoundException("Member was not found.");

        return ToMemberDetailResponse(member);
    }

    public async Task<MemberDetailResponse> GetMemberAsync(string gymCode, Guid id)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            App.Domain.RoleNames.GymOwner,
            App.Domain.RoleNames.GymAdmin,
            App.Domain.RoleNames.Member);

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, id);

        var member = await dbContext.Members
            .Include(entity => entity.Person)
            .FirstOrDefaultAsync(entity => entity.Id == id)
            ?? throw new AppNotFoundException("Member was not found.");

        return ToMemberDetailResponse(member);
    }

    public async Task<MemberDetailResponse> CreateMemberAsync(string gymCode, MemberUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        var person = new Person
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PersonalCode = request.PersonalCode?.Trim(),
            DateOfBirth = request.DateOfBirth
        };

        var member = new Member
        {
            GymId = gymId,
            Person = person,
            MemberCode = request.MemberCode.Trim(),
            Status = request.Status
        };

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        return ToMemberDetailResponse(member);
    }

    public async Task<MemberDetailResponse> UpdateMemberAsync(string gymCode, Guid id, MemberUpsertRequest request)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        var member = await dbContext.Members
            .Include(entity => entity.Person)
            .FirstOrDefaultAsync(entity => entity.Id == id)
            ?? throw new AppNotFoundException("Member was not found.");

        member.MemberCode = request.MemberCode.Trim();
        member.Status = request.Status;
        member.Person!.FirstName = request.FirstName.Trim();
        member.Person.LastName = request.LastName.Trim();
        member.Person.PersonalCode = request.PersonalCode?.Trim();
        member.Person.DateOfBirth = request.DateOfBirth;

        await dbContext.SaveChangesAsync();

        return ToMemberDetailResponse(member);
    }

    public async Task DeleteMemberAsync(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        var member = await dbContext.Members.FirstOrDefaultAsync(entity => entity.Id == id)
                     ?? throw new AppNotFoundException("Member was not found.");

        dbContext.Members.Remove(member);
        await dbContext.SaveChangesAsync();
    }

    private static MemberDetailResponse ToMemberDetailResponse(Member member)
    {
        return new MemberDetailResponse
        {
            Id = member.Id,
            MemberCode = member.MemberCode,
            FirstName = member.Person?.FirstName ?? string.Empty,
            LastName = member.Person?.LastName ?? string.Empty,
            FullName = $"{member.Person?.FirstName} {member.Person?.LastName}".Trim(),
            PersonalCode = member.Person?.PersonalCode,
            DateOfBirth = member.Person?.DateOfBirth,
            Status = member.Status
        };
    }
}
