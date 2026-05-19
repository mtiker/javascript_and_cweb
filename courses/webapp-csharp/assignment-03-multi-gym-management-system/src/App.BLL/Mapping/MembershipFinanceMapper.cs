using System.Globalization;
using App.Domain.Common;
using App.Domain.Entities;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;

namespace App.BLL.Mapping;

public sealed class MembershipFinanceMapper : IMembershipFinanceMapper
{
    public MembershipPackageResponse ToPackageResponse(MembershipPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        return new MembershipPackageResponse
        {
            Id = package.Id,
            Name = Translate(package.Name) ?? package.Name.ToString(),
            PackageType = package.PackageType,
            DurationValue = package.DurationValue,
            DurationUnit = package.DurationUnit,
            BasePrice = package.BasePrice,
            CurrencyCode = package.CurrencyCode,
            TrainingDiscountPercent = package.TrainingDiscountPercent,
            IsTrainingFree = package.IsTrainingFree,
            Description = Translate(package.Description) ?? package.Description?.ToString()
        };
    }

    public IReadOnlyCollection<MembershipPackageResponse> ToPackageResponses(IEnumerable<MembershipPackage> packages) =>
        packages.Select(ToPackageResponse).ToArray();

    public MembershipResponse ToMembershipResponse(Membership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);
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

    public IReadOnlyCollection<MembershipResponse> ToMembershipResponses(IEnumerable<Membership> memberships) =>
        memberships.Select(ToMembershipResponse).ToArray();

    public MembershipAdminSummaryResponse ToAdminSummary(Membership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);
        return new MembershipAdminSummaryResponse
        {
            Id = membership.Id,
            MemberName = $"{membership.Member?.Person?.FirstName} {membership.Member?.Person?.LastName}".Trim(),
            PackageName = Translate(membership.MembershipPackage?.Name) ?? membership.MembershipPackage?.Name.ToString() ?? string.Empty,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            Status = membership.Status
        };
    }

    public IReadOnlyCollection<MembershipAdminSummaryResponse> ToAdminSummaries(IEnumerable<Membership> memberships) =>
        memberships.Select(ToAdminSummary).ToArray();

    public PaymentResponse ToPaymentResponse(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
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

    public IReadOnlyCollection<PaymentResponse> ToPaymentResponses(IEnumerable<Payment> payments) =>
        payments.Select(ToPaymentResponse).ToArray();

    public LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
