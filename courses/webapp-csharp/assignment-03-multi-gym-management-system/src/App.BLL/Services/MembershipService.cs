using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Memberships;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class MembershipService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService) : IMembershipService
{
    public async Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var query = dbContext.Memberships.Where(entity => entity.GymId == gymId);
        var member = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);

        if (member != null)
        {
            query = query.Where(entity => entity.MemberId == member.Id);
        }

        return await query
            .OrderByDescending(entity => entity.StartDate)
            .Select(entity => MembershipWorkflowMapping.ToMembershipResponse(entity))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        var member = await dbContext.Members.FirstOrDefaultAsync(entity => entity.Id == request.MemberId, cancellationToken)
                     ?? throw new NotFoundException("Member was not found.");

        var package = await dbContext.MembershipPackages.FirstOrDefaultAsync(entity => entity.Id == request.MembershipPackageId, cancellationToken)
                      ?? throw new NotFoundException("Membership package was not found.");

        var startDate = request.RequestedStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var endDate = MembershipWorkflowMapping.CalculateMembershipEndDate(startDate, package.DurationValue, package.DurationUnit);

        var overlappingMemberships = await dbContext.Memberships
            .Where(entity => entity.GymId == gymId && entity.MemberId == member.Id)
            .Where(entity => entity.StartDate <= endDate && entity.EndDate >= startDate)
            .OrderByDescending(entity => entity.EndDate)
            .ToListAsync(cancellationToken);

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
                                    await dbContext.Memberships.AnyAsync(
                                        entity => entity.GymId == gymId && entity.MemberId == member.Id,
                                        cancellationToken);

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

        dbContext.Memberships.Add(membership);

        if (package.BasePrice > 0)
        {
            dbContext.Payments.Add(new Payment
            {
                GymId = gymId,
                Membership = membership,
                Amount = package.BasePrice,
                CurrencyCode = package.CurrencyCode,
                Status = PaymentStatus.Completed,
                Reference = request.PaymentReference ?? $"MEM-{DateTime.UtcNow:yyyyMMddHHmmss}"
            });
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

        var membership = await dbContext.Memberships.FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken)
                         ?? throw new NotFoundException("Membership was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);
        MembershipWorkflowMapping.EnsureMembershipStatusTransition(membership.Status, request.Status);

        membership.Status = request.Status;

        if (request.Status == MembershipStatus.Expired && membership.EndDate > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            membership.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return MembershipWorkflowMapping.ToMembershipResponse(membership);
    }

    public async Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var membership = await dbContext.Memberships.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
                         ?? throw new NotFoundException("Membership was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);

        dbContext.Memberships.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
