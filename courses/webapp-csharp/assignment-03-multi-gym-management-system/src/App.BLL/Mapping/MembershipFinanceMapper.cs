using System.Globalization;
using App.Domain.Common;
using App.Domain.Entities;
using App.DTO.v1.Finance;
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
            PackageName = Translate(membership.MembershipPackage?.Name) ?? string.Empty,
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

    public InvoiceResponse ToInvoiceResponse(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        return new InvoiceResponse
        {
            Id = invoice.Id,
            MemberId = invoice.MemberId,
            MemberName = $"{invoice.Member?.Person?.FirstName} {invoice.Member?.Person?.LastName}".Trim(),
            InvoiceNumber = invoice.InvoiceNumber,
            IssuedAtUtc = invoice.IssuedAtUtc,
            DueAtUtc = invoice.DueAtUtc,
            CurrencyCode = invoice.CurrencyCode,
            SubtotalAmount = invoice.SubtotalAmount,
            CreditAmount = invoice.CreditAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            OutstandingAmount = invoice.OutstandingAmount,
            IsOverdue = invoice.OutstandingAmount > 0 && invoice.DueAtUtc < DateTime.UtcNow,
            Status = invoice.Status,
            Notes = invoice.Notes,
            Lines = invoice.Lines
                .OrderBy(line => line.CreatedAtUtc)
                .Select(line => new InvoiceLineResponse
                {
                    Id = line.Id,
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    LineTotal = line.LineTotal,
                    IsCredit = line.IsCredit,
                    Notes = line.Notes
                })
                .ToArray(),
            Payments = invoice.Payments
                .OrderByDescending(payment => payment.AppliedAtUtc)
                .Select(payment => new InvoicePaymentResponse
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    IsRefund = payment.IsRefund,
                    AppliedAtUtc = payment.AppliedAtUtc,
                    Reference = payment.Reference,
                    Notes = payment.Notes
                })
                .ToArray()
        };
    }

    public IReadOnlyCollection<InvoiceResponse> ToInvoiceResponses(IEnumerable<Invoice> invoices) =>
        invoices.Select(ToInvoiceResponse).ToArray();

    public FinanceWorkspaceResponse ToFinanceWorkspace(Member member, IReadOnlyCollection<InvoiceResponse> invoices)
    {
        ArgumentNullException.ThrowIfNull(member);
        var paymentHistory = invoices
            .SelectMany(invoice => invoice.Payments)
            .OrderByDescending(payment => payment.AppliedAtUtc)
            .ToArray();

        return new FinanceWorkspaceResponse
        {
            MemberId = member.Id,
            MemberName = $"{member.Person?.FirstName} {member.Person?.LastName}".Trim(),
            MemberCode = member.MemberCode,
            OutstandingBalance = invoices.Sum(invoice => invoice.OutstandingAmount),
            TotalRefundCredits = paymentHistory.Where(payment => payment.IsRefund).Sum(payment => payment.Amount),
            OverdueInvoiceCount = invoices.Count(invoice => invoice.IsOverdue),
            Invoices = invoices,
            PaymentHistory = paymentHistory
        };
    }

    public LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    private static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
