using App.DAL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mappers;
using App.Domain.Entities;
using App.DTO.v1.Members;

namespace App.BLL.Services;

public class MemberWorkflowService(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    ISubscriptionTierLimitService subscriptionTierLimitService,
    IMemberMapper memberMapper) : IMemberWorkflowService
{
    public async Task<IReadOnlyCollection<MemberResponse>> GetMembersAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        var members = await unitOfWork.Members.ListByGymAsync(gymId, cancellationToken);
        return memberMapper.ToSummaryList(members);
    }

    public async Task<MemberDetailResponse> GetCurrentMemberAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            App.Domain.RoleNames.GymOwner,
            App.Domain.RoleNames.GymAdmin,
            App.Domain.RoleNames.Member);

        var currentMember = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken)
                            ?? throw new NotFoundException("Current user does not have a member profile in the active gym.");

        var member = await unitOfWork.Members.FindWithPersonAsync(gymId, currentMember.Id, cancellationToken)
                     ?? throw new NotFoundException("Member was not found.");

        return memberMapper.ToDetail(member);
    }

    public async Task<MemberDetailResponse> GetMemberAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            App.Domain.RoleNames.GymOwner,
            App.Domain.RoleNames.GymAdmin,
            App.Domain.RoleNames.Member);

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, id, cancellationToken);

        var member = await unitOfWork.Members.FindWithPersonAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Member was not found.");

        return memberMapper.ToDetail(member);
    }

    public async Task<MemberDetailResponse> CreateMemberAsync(string gymCode, MemberUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        await subscriptionTierLimitService.EnsureCanCreateMemberAsync(gymId, cancellationToken);
        var normalized = NormalizeRequest(request);

        await EnsureUniqueMemberFieldsAsync(gymId, normalized.MemberCode, normalized.PersonalCode, null, null, cancellationToken);

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

        await unitOfWork.Members.AddAsync(member, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return memberMapper.ToDetail(member);
    }

    public async Task<MemberDetailResponse> UpdateMemberAsync(string gymCode, Guid id, MemberUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var normalized = NormalizeRequest(request);

        var member = await unitOfWork.Members.FindWithPersonAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Member was not found.");

        await EnsureUniqueMemberFieldsAsync(gymId, normalized.MemberCode, normalized.PersonalCode, member.Id, member.PersonId, cancellationToken);

        member.MemberCode = normalized.MemberCode;
        member.Status = normalized.Status;
        member.Person!.FirstName = normalized.FirstName;
        member.Person.LastName = normalized.LastName;
        member.Person.PersonalCode = normalized.PersonalCode;
        member.Person.DateOfBirth = normalized.DateOfBirth;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return memberMapper.ToDetail(member);
    }

    public async Task DeleteMemberAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);

        var member = await unitOfWork.Members.FindAsync(gymId, id, cancellationToken)
                     ?? throw new NotFoundException("Member was not found.");

        unitOfWork.Members.Remove(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueMemberFieldsAsync(
        Guid gymId,
        string memberCode,
        string? personalCode,
        Guid? currentMemberId,
        Guid? currentPersonId,
        CancellationToken cancellationToken)
    {
        var memberCodeExists = await unitOfWork.Members.MemberCodeExistsAsync(gymId, memberCode, currentMemberId, cancellationToken);
        if (memberCodeExists)
        {
            throw new ValidationAppException("Member code already exists in this gym.");
        }

        if (string.IsNullOrWhiteSpace(personalCode))
        {
            return;
        }

        var personalCodeExists = await unitOfWork.Members.PersonalCodeExistsAsync(personalCode, currentPersonId, cancellationToken);
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
