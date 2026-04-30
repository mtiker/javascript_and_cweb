using App.Domain.Common;
using App.Domain.Entities;
using App.DTO.v1.Finance;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;

namespace App.BLL.Mapping;

public interface IMembershipFinanceMapper
{
    MembershipPackageResponse ToPackageResponse(MembershipPackage package);
    IReadOnlyCollection<MembershipPackageResponse> ToPackageResponses(IEnumerable<MembershipPackage> packages);
    MembershipResponse ToMembershipResponse(Membership membership);
    IReadOnlyCollection<MembershipResponse> ToMembershipResponses(IEnumerable<Membership> memberships);
    MembershipAdminSummaryResponse ToAdminSummary(Membership membership);
    IReadOnlyCollection<MembershipAdminSummaryResponse> ToAdminSummaries(IEnumerable<Membership> memberships);
    PaymentResponse ToPaymentResponse(Payment payment);
    IReadOnlyCollection<PaymentResponse> ToPaymentResponses(IEnumerable<Payment> payments);
    InvoiceResponse ToInvoiceResponse(Invoice invoice);
    IReadOnlyCollection<InvoiceResponse> ToInvoiceResponses(IEnumerable<Invoice> invoices);
    FinanceWorkspaceResponse ToFinanceWorkspace(Member member, IReadOnlyCollection<InvoiceResponse> invoices);
    LangStr ToLangStr(string value);
}
