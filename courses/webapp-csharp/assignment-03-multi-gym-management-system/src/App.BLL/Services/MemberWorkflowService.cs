using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using App.DTO.v1.Members;

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
                            ?? throw new NotFoundException("Current user does not have a member profile in the active gym.");

        var member = await dbContext.Members
            .Include(entity => entity.Person)
            .FirstOrDefaultAsync(entity => entity.Id == currentMember.Id)
            ?? throw new NotFoundException("Member was not found.");

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
            ?? throw new NotFoundException("Member was not found.");

        return ToMemberDetailResponse(member);
    }

    public async Task<MemberDetailResponse> CreateMemberAsync(string gymCode, MemberUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var normalized = NormalizeRequest(request);

        await EnsureUniqueMemberFieldsAsync(gymId, normalized.MemberCode, normalized.PersonalCode, null, null);

        var person = new Person
        {
            FirstName = normalized.FirstName,
            LastName = normalized.LastName,
            PersonalCode = normalized.PersonalCode,
            DateOfBirth = normalized.DateOfBirth
        };

        var member = new Member
        {
            GymId = gymId,
            Person = person,
            MemberCode = normalized.MemberCode,
            Status = normalized.Status
        };

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        return ToMemberDetailResponse(member);
    }

    public async Task<MemberDetailResponse> UpdateMemberAsync(string gymCode, Guid id, MemberUpsertRequest request)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var normalized = NormalizeRequest(request);

        var member = await dbContext.Members
            .Include(entity => entity.Person)
            .FirstOrDefaultAsync(entity => entity.Id == id)
            ?? throw new NotFoundException("Member was not found.");

        await EnsureUniqueMemberFieldsAsync(gymId, normalized.MemberCode, normalized.PersonalCode, member.Id, member.PersonId);

        member.MemberCode = normalized.MemberCode;
        member.Status = normalized.Status;
        member.Person!.FirstName = normalized.FirstName;
        member.Person.LastName = normalized.LastName;
        member.Person.PersonalCode = normalized.PersonalCode;
        member.Person.DateOfBirth = normalized.DateOfBirth;

        await dbContext.SaveChangesAsync();

        return ToMemberDetailResponse(member);
    }

    public async Task DeleteMemberAsync(string gymCode, Guid id)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        var member = await dbContext.Members.FirstOrDefaultAsync(entity => entity.Id == id)
                     ?? throw new NotFoundException("Member was not found.");

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

    private async Task EnsureUniqueMemberFieldsAsync(
        Guid gymId,
        string memberCode,
        string? personalCode,
        Guid? currentMemberId,
        Guid? currentPersonId)
    {
        var memberCodeExists = await dbContext.Members.AnyAsync(entity =>
            entity.GymId == gymId &&
            entity.MemberCode == memberCode &&
            (!currentMemberId.HasValue || entity.Id != currentMemberId.Value));

        if (memberCodeExists)
        {
            throw new ValidationAppException("Member code already exists in this gym.");
        }

        if (string.IsNullOrWhiteSpace(personalCode))
        {
            return;
        }

        var personalCodeExists = await dbContext.People.AnyAsync(entity =>
            entity.PersonalCode == personalCode &&
            (!currentPersonId.HasValue || entity.Id != currentPersonId.Value));

        if (personalCodeExists)
        {
            throw new ValidationAppException("Personal code already belongs to another person.");
        }
    }

    private static MemberUpsertRequest NormalizeRequest(MemberUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            throw new ValidationAppException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationAppException("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.MemberCode))
        {
            throw new ValidationAppException("Member code is required.");
        }

        return new MemberUpsertRequest
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PersonalCode = string.IsNullOrWhiteSpace(request.PersonalCode) ? null : request.PersonalCode.Trim(),
            DateOfBirth = request.DateOfBirth,
            MemberCode = request.MemberCode.Trim(),
            Status = request.Status
        };
    }
}
