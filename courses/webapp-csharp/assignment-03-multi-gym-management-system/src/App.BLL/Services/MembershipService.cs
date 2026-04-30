using App.BLL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mapping;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Memberships;

namespace App.BLL.Services;

public class MembershipService(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    IMembershipFinanceMapper mapper) : IMembershipService
{
    public async Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var member = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);
        var memberships = member == null
            ? await unitOfWork.Memberships.ListByGymAsync(gymId, cancellationToken)
            : await unitOfWork.Memberships.ListForMemberAsync(gymId, member.Id, cancellationToken);

        return mapper.ToMembershipResponses(memberships);
    }

    public async Task<IReadOnlyCollection<MembershipAdminSummaryResponse>> GetActiveMembershipSummariesAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var memberships = await unitOfWork.Memberships.ListActiveWithDetailsAsync(gymId, cancellationToken);
        return mapper.ToAdminSummaries(memberships);
    }

    public async Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        var member = await unitOfWork.Members.FindAsync(gymId, request.MemberId, cancellationToken)
                     ?? throw new NotFoundException("Member was not found.");

        var package = await unitOfWork.MembershipPackages.FindAsync(gymId, request.MembershipPackageId, cancellationToken)
                      ?? throw new NotFoundException("Membership package was not found.");

        var startDate = request.RequestedStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var endDate = MembershipWorkflowMapping.CalculateMembershipEndDate(startDate, package.DurationValue, package.DurationUnit);

        var overlappingMemberships = await unitOfWork.Memberships.ListOverlappingAsync(gymId, member.Id, startDate, endDate, cancellationToken);

        if (overlappingMemberships.Count > 0)
        {
            return new MembershipSaleResponse
            {
                MembershipId = Guid.Empty,
                StartDate = startDate,
                EndDate = endDate,
                OverlapDetected = true,
                SuggestedStartDate = overlappingMemberships.Max(entity => entity.EndDate).AddDays(1)
            };
        }

        var hasPreviousMembership = overlappingMemberships.Count == 0 &&
                                    await unitOfWork.Memberships.ExistsForMemberAsync(gymId, member.Id, cancellationToken);

        var membership = new Membership
        {
            GymId = gymId,
            MemberId = member.Id,
            MembershipPackageId = package.Id,
            StartDate = startDate,
            EndDate = endDate,
            PriceAtPurchase = package.BasePrice,
            CurrencyCode = package.CurrencyCode,
            Status = MembershipWorkflowMapping.ResolveInitialStatus(startDate, hasPreviousMembership)
        };

        await unitOfWork.Memberships.AddAsync(membership, cancellationToken);

        if (package.BasePrice > 0)
        {
            await unitOfWork.Payments.AddAsync(new Payment
            {
                GymId = gymId,
                Membership = membership,
                Amount = package.BasePrice,
                CurrencyCode = package.CurrencyCode,
                Status = PaymentStatus.Completed,
                Reference = request.PaymentReference ?? $"MEM-{DateTime.UtcNow:yyyyMMddHHmmss}"
            }, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MembershipSaleResponse
        {
            MembershipId = membership.Id,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            OverlapDetected = false
        };
    }

    public async Task<MembershipResponse> UpdateMembershipStatusAsync(string gymCode, Guid id, MembershipStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var membership = await unitOfWork.Memberships.FindAsync(gymId, id, cancellationToken)
                         ?? throw new NotFoundException("Membership was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);
        MembershipWorkflowMapping.EnsureMembershipStatusTransition(membership.Status, request.Status);

        membership.Status = request.Status;

        if (request.Status == MembershipStatus.Expired && membership.EndDate > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            membership.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.ToMembershipResponse(membership);
    }

    public async Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var membership = await unitOfWork.Memberships.FindAsync(gymId, id, cancellationToken)
                         ?? throw new NotFoundException("Membership was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);

        unitOfWork.Memberships.Remove(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
