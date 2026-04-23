using System.Globalization;
using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;

namespace App.BLL.Services;

public class MembershipWorkflowService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService) : IMembershipWorkflowService
{
    public async Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);

        return await dbContext.MembershipPackages
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.ValidFrom)
            .Select(entity => new MembershipPackageResponse
            {
                Id = entity.Id,
                Name = Translate(entity.Name) ?? string.Empty,
                PackageType = entity.PackageType,
                DurationValue = entity.DurationValue,
                DurationUnit = entity.DurationUnit,
                BasePrice = entity.BasePrice,
                CurrencyCode = entity.CurrencyCode,
                TrainingDiscountPercent = entity.TrainingDiscountPercent,
                IsTrainingFree = entity.IsTrainingFree,
                Description = Translate(entity.Description)
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var package = new MembershipPackage
        {
            GymId = gymId,
            Name = ToLangStr(request.Name),
            PackageType = request.PackageType,
            DurationValue = request.DurationValue,
            DurationUnit = request.DurationUnit,
            BasePrice = request.BasePrice,
            CurrencyCode = request.CurrencyCode,
            TrainingDiscountPercent = request.TrainingDiscountPercent,
            IsTrainingFree = request.IsTrainingFree,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description)
        };

        dbContext.MembershipPackages.Add(package);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPackageResponse(package);
    }

    public async Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var package = await dbContext.MembershipPackages.FirstOrDefaultAsync(entity => entity.Id == id)
                      ?? throw new NotFoundException("Membership package was not found.");

        package.Name = ToLangStr(request.Name);
        package.PackageType = request.PackageType;
        package.DurationValue = request.DurationValue;
        package.DurationUnit = request.DurationUnit;
        package.BasePrice = request.BasePrice;
        package.CurrencyCode = request.CurrencyCode;
        package.TrainingDiscountPercent = request.TrainingDiscountPercent;
        package.IsTrainingFree = request.IsTrainingFree;
        package.Description = string.IsNullOrWhiteSpace(request.Description) ? null : ToLangStr(request.Description);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPackageResponse(package);
    }

    public async Task DeletePackageAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var package = await dbContext.MembershipPackages.FirstOrDefaultAsync(entity => entity.Id == id)
                      ?? throw new NotFoundException("Membership package was not found.");
        dbContext.MembershipPackages.Remove(package);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);
        var query = dbContext.Memberships.Where(entity => entity.GymId == gymId);

        // The current user context is evaluated inside the authorization service for individual writes.
        // For member reads, narrow the result by resolving the current member when available.
        var member = await authorizationService.GetCurrentMemberAsync(gymId);
        if (member != null)
        {
            query = query.Where(entity => entity.MemberId == member.Id);
        }

        return await query
            .OrderByDescending(entity => entity.StartDate)
            .Select(entity => new MembershipResponse
            {
                Id = entity.Id,
                MemberId = entity.MemberId,
                MembershipPackageId = entity.MembershipPackageId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                PriceAtPurchase = entity.PriceAtPurchase,
                CurrencyCode = entity.CurrencyCode,
                Status = entity.Status
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var member = await dbContext.Members.FirstOrDefaultAsync(entity => entity.Id == request.MemberId)
                     ?? throw new NotFoundException("Member was not found.");
        var package = await dbContext.MembershipPackages.FirstOrDefaultAsync(entity => entity.Id == request.MembershipPackageId)
                      ?? throw new NotFoundException("Membership package was not found.");

        var startDate = request.RequestedStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var endDate = CalculateMembershipEndDate(startDate, package.DurationValue, package.DurationUnit);

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

        var membership = new Membership
        {
            GymId = gymId,
            MemberId = member.Id,
            MembershipPackageId = package.Id,
            StartDate = startDate,
            EndDate = endDate,
            PriceAtPurchase = package.BasePrice,
            CurrencyCode = package.CurrencyCode,
            Status = ResolveInitialStatus(startDate, overlappingMemberships.Count == 0 &&
                await dbContext.Memberships.AnyAsync(entity => entity.GymId == gymId && entity.MemberId == member.Id, cancellationToken))
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
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);
        var membership = await dbContext.Memberships.FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken)
                         ?? throw new NotFoundException("Membership was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId, cancellationToken);
        EnsureMembershipStatusTransition(membership.Status, request.Status);

        membership.Status = request.Status;

        if (request.Status == MembershipStatus.Expired && membership.EndDate > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            membership.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new MembershipResponse
        {
            Id = membership.Id,
            MemberId = membership.MemberId,
            MembershipPackageId = membership.MembershipPackageId,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            PriceAtPurchase = membership.PriceAtPurchase,
            CurrencyCode = membership.CurrencyCode,
            Status = membership.Status
        };
    }

    public async Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);

        if (!request.MembershipId.HasValue && !request.BookingId.HasValue)
        {
            throw new ValidationAppException("Payment must be linked to a membership or a booking.");
        }

        if (request.MembershipId.HasValue)
        {
            var membership = await dbContext.Memberships.FirstOrDefaultAsync(entity => entity.Id == request.MembershipId.Value)
                             ?? throw new NotFoundException("Membership was not found.");
            await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId);
        }

        if (request.BookingId.HasValue)
        {
            var booking = await dbContext.Bookings.FirstOrDefaultAsync(entity => entity.Id == request.BookingId.Value)
                          ?? throw new NotFoundException("Booking was not found.");
            await authorizationService.EnsureBookingAccessAsync(booking);
        }

        var payment = new Payment
        {
            GymId = gymId,
            MembershipId = request.MembershipId,
            BookingId = request.BookingId,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            Reference = request.Reference,
            Status = PaymentStatus.Completed
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PaymentResponse
        {
            Id = payment.Id,
            Amount = payment.Amount,
            CurrencyCode = payment.CurrencyCode,
            PaidAtUtc = payment.PaidAtUtc,
            Status = payment.Status,
            Reference = payment.Reference,
            MembershipId = payment.MembershipId,
            BookingId = payment.BookingId
        };
    }

    public async Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);
        var membership = await dbContext.Memberships.FirstOrDefaultAsync(entity => entity.Id == id)
                         ?? throw new NotFoundException("Membership was not found.");
        await authorizationService.EnsureMemberSelfAccessAsync(gymId, membership.MemberId);
        dbContext.Memberships.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member);
        var query = dbContext.Payments.Where(entity => entity.GymId == gymId);

        var member = await authorizationService.GetCurrentMemberAsync(gymId);
        if (member != null)
        {
            var membershipIds = await dbContext.Memberships.Where(entity => entity.MemberId == member.Id).Select(entity => entity.Id).ToListAsync(cancellationToken);
            var bookingIds = await dbContext.Bookings.Where(entity => entity.MemberId == member.Id).Select(entity => entity.Id).ToListAsync(cancellationToken);
            query = query.Where(entity => entity.MembershipId.HasValue && membershipIds.Contains(entity.MembershipId.Value)
                                          || entity.BookingId.HasValue && bookingIds.Contains(entity.BookingId.Value));
        }

        return await query
            .OrderByDescending(entity => entity.PaidAtUtc)
            .Select(entity => new PaymentResponse
            {
                Id = entity.Id,
                Amount = entity.Amount,
                CurrencyCode = entity.CurrencyCode,
                PaidAtUtc = entity.PaidAtUtc,
                Status = entity.Status,
                Reference = entity.Reference,
                MembershipId = entity.MembershipId,
                BookingId = entity.BookingId
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession, CancellationToken cancellationToken = default)
    {
        var bookingDate = DateOnly.FromDateTime(trainingSession.StartAtUtc.Date);
        var membership = await dbContext.Memberships
            .Include(entity => entity.MembershipPackage)
            .Where(entity => entity.GymId == gymId && entity.MemberId == memberId)
            .Where(entity => entity.StartDate <= bookingDate && entity.EndDate >= bookingDate)
            .OrderByDescending(entity => entity.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership?.MembershipPackage == null)
        {
            return trainingSession.BasePrice;
        }

        if (membership.MembershipPackage.IsTrainingFree)
        {
            return 0m;
        }

        if (membership.MembershipPackage.TrainingDiscountPercent is > 0)
        {
            var percent = membership.MembershipPackage.TrainingDiscountPercent.Value;
            return Math.Round(trainingSession.BasePrice * (100 - percent) / 100m, 2, MidpointRounding.AwayFromZero);
        }

        return trainingSession.BasePrice;
    }

    private static DateOnly CalculateMembershipEndDate(DateOnly startDate, int durationValue, DurationUnit durationUnit)
    {
        return durationUnit switch
        {
            DurationUnit.Day => startDate.AddDays(durationValue - 1),
            DurationUnit.Month => startDate.AddMonths(durationValue).AddDays(-1),
            DurationUnit.Year => startDate.AddYears(durationValue).AddDays(-1),
            _ => startDate
        };
    }

    private static MembershipStatus ResolveInitialStatus(DateOnly startDate, bool hasPreviousMembership)
    {
        if (startDate > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            return MembershipStatus.Pending;
        }

        return hasPreviousMembership
            ? MembershipStatus.Renewed
            : MembershipStatus.Active;
    }

    private static void EnsureMembershipStatusTransition(MembershipStatus current, MembershipStatus next)
    {
        if (current == next)
        {
            return;
        }

        var allowed = current switch
        {
            MembershipStatus.Pending => next is MembershipStatus.Active or MembershipStatus.Cancelled,
            MembershipStatus.Active => next is MembershipStatus.Paused or MembershipStatus.Expired or MembershipStatus.Cancelled or MembershipStatus.Refunded or MembershipStatus.Renewed,
            MembershipStatus.Paused => next is MembershipStatus.Active or MembershipStatus.Cancelled or MembershipStatus.Expired,
            MembershipStatus.Expired => next is MembershipStatus.Renewed or MembershipStatus.Cancelled,
            MembershipStatus.Cancelled => next is MembershipStatus.Renewed,
            MembershipStatus.Refunded => next is MembershipStatus.Renewed or MembershipStatus.Cancelled,
            MembershipStatus.Renewed => next is MembershipStatus.Active or MembershipStatus.Paused or MembershipStatus.Expired or MembershipStatus.Cancelled,
            _ => false
        };

        if (!allowed)
        {
            throw new ValidationAppException($"Invalid membership status transition from {current} to {next}.");
        }
    }

    private static MembershipPackageResponse ToPackageResponse(MembershipPackage package)
    {
        return new MembershipPackageResponse
        {
            Id = package.Id,
            Name = Translate(package.Name) ?? string.Empty,
            PackageType = package.PackageType,
            DurationValue = package.DurationValue,
            DurationUnit = package.DurationUnit,
            BasePrice = package.BasePrice,
            CurrencyCode = package.CurrencyCode,
            TrainingDiscountPercent = package.TrainingDiscountPercent,
            IsTrainingFree = package.IsTrainingFree,
            Description = Translate(package.Description)
        };
    }

    private static LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
