using App.BLL.Contracts.Infrastructure;
using App.BLL.Contracts.Services;
using SharedKernel.Exceptions;
using Modules.Memberships.Application.Mappers;
using SharedKernel;
using App.Domain.Entities;
using Modules.Memberships.Application.Persistence;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Memberships;

namespace Modules.Memberships.Application;

public class MembershipService(
    IAppDbContext dbContext,
    IMemberRepository memberRepository,
    IMembershipPackageRepository membershipPackageRepository,
    IMembershipRepository membershipRepository,
    IPaymentRepository paymentRepository,
    IAuthorizationService authorizationService,
    IMembershipFinanceMapper mapper) : IMembershipService
{
    public async Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, MembershipFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var member = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);
        var hasFilter = filter is not null && (filter.Status.HasValue || filter.MemberId.HasValue || filter.MembershipPackageId.HasValue || filter.StartFrom.HasValue || filter.StartTo.HasValue);

        IReadOnlyList<Membership> memberships;
        if (member != null)
        {
            memberships = await membershipRepository.ListForMemberAsync(gymId, member.Id, cancellationToken);
            if (hasFilter)
            {
                memberships = ApplyMembershipFilterInMemory(memberships, filter!);
            }
        }
        else if (hasFilter)
        {
            memberships = await membershipRepository.ListByGymFilteredAsync(gymId, filter!.Status, filter.MemberId, filter.MembershipPackageId, filter.StartFrom, filter.StartTo, cancellationToken);
        }
        else
        {
            memberships = await membershipRepository.ListByGymAsync(gymId, cancellationToken);
        }

        return mapper.ToMembershipResponses(memberships);
    }

    private static IReadOnlyList<Membership> ApplyMembershipFilterInMemory(IReadOnlyList<Membership> memberships, MembershipFilter filter)
    {
        IEnumerable<Membership> q = memberships;
        if (filter.Status.HasValue) q = q.Where(m => m.Status == filter.Status.Value);
        if (filter.MemberId.HasValue) q = q.Where(m => m.MemberId == filter.MemberId.Value);
        if (filter.MembershipPackageId.HasValue) q = q.Where(m => m.MembershipPackageId == filter.MembershipPackageId.Value);
        if (filter.StartFrom.HasValue) q = q.Where(m => m.StartDate >= filter.StartFrom.Value);
        if (filter.StartTo.HasValue) q = q.Where(m => m.StartDate <= filter.StartTo.Value);
        return q.ToArray();
    }

    public async Task<MembershipResponse> UpdateMembershipAsync(string gymCode, Guid id, MembershipEditRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        if (request.EndDate < request.StartDate)
        {
            throw new ValidationAppException("End date must be on or after start date.");
        }

        var membership = await membershipRepository.FindAsync(gymId, id, cancellationToken)
                         ?? throw new NotFoundException("Membership was not found.");

        var package = await membershipPackageRepository.FindAsync(gymId, request.MembershipPackageId, cancellationToken)
                      ?? throw new NotFoundException("Membership package was not found.");

        if (membership.Status is MembershipStatus.Cancelled or MembershipStatus.Refunded or MembershipStatus.Expired)
        {
            throw new ValidationAppException("This membership can no longer be edited.");
        }

        membership.MembershipPackageId = package.Id;
        membership.StartDate = request.StartDate;
        membership.EndDate = request.EndDate;

        await dbContext.SaveChangesAsync(cancellationToken);
        return mapper.ToMembershipResponse(membership);
    }

    public async Task<IReadOnlyCollection<MembershipAdminSummaryResponse>> GetActiveMembershipSummariesAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var memberships = await membershipRepository.ListActiveWithDetailsAsync(gymId, cancellationToken);
        return mapper.ToAdminSummaries(memberships);
    }

    public async Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        var member = await memberRepository.FindAsync(gymId, request.MemberId, cancellationToken)
                     ?? throw new NotFoundException("Member was not found.");

        var package = await membershipPackageRepository.FindAsync(gymId, request.MembershipPackageId, cancellationToken)
                      ?? throw new NotFoundException("Membership package was not found.");

        var startDate = request.RequestedStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var endDate = MembershipWorkflowMapping.CalculateMembershipEndDate(startDate, package.DurationValue, package.DurationUnit);

        var overlappingMemberships = await membershipRepository.ListOverlappingAsync(gymId, member.Id, startDate, endDate, cancellationToken);

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
                                    await membershipRepository.ExistsForMemberAsync(gymId, member.Id, cancellationToken);

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

        await membershipRepository.AddAsync(membership, cancellationToken);

        if (package.BasePrice > 0)
        {
            await paymentRepository.AddAsync(new Payment
            {
                GymId = gymId,
                Membership = membership,
                Amount = package.BasePrice,
                CurrencyCode = package.CurrencyCode,
                Status = PaymentStatus.Completed,
                Reference = request.PaymentReference ?? $"MEM-{DateTime.UtcNow:yyyyMMddHHmmss}"
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

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

        var membership = await membershipRepository.FindAsync(gymId, id, cancellationToken)
                         ?? throw new NotFoundException("Membership was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);
        MembershipWorkflowMapping.EnsureMembershipStatusTransition(membership.Status, request.Status);

        membership.Status = request.Status;

        if (request.Status == MembershipStatus.Expired && membership.EndDate > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            membership.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

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

        var membership = await membershipRepository.FindAsync(gymId, id, cancellationToken)
                         ?? throw new NotFoundException("Membership was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);

        membershipRepository.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
