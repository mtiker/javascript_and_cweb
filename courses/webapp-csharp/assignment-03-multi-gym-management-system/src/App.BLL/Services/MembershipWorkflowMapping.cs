using System.Globalization;
using App.BLL.Exceptions;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;

namespace App.BLL.Services;

internal static class MembershipWorkflowMapping
{
    public static MembershipPackageResponse ToPackageResponse(MembershipPackage package)
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

    public static MembershipResponse ToMembershipResponse(Membership membership)
    {
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

    public static PaymentResponse ToPaymentResponse(Payment payment)
    {
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

    public static LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    public static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }

    public static DateOnly CalculateMembershipEndDate(DateOnly startDate, int durationValue, DurationUnit durationUnit)
    {
        return durationUnit switch
        {
            DurationUnit.Day => startDate.AddDays(durationValue - 1),
            DurationUnit.Month => startDate.AddMonths(durationValue).AddDays(-1),
            DurationUnit.Year => startDate.AddYears(durationValue).AddDays(-1),
            _ => startDate
        };
    }

    public static MembershipStatus ResolveInitialStatus(DateOnly startDate, bool hasPreviousMembership)
    {
        if (startDate > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            return MembershipStatus.Pending;
        }

        return hasPreviousMembership
            ? MembershipStatus.Renewed
            : MembershipStatus.Active;
    }

    public static void EnsureMembershipStatusTransition(MembershipStatus current, MembershipStatus next)
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
}
