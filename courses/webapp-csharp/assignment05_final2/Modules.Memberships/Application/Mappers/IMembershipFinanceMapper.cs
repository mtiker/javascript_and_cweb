using Base.Domain;
using App.Domain.Entities;
using Shared.Contracts.Dtos.v1.MembershipPackages;
using Shared.Contracts.Dtos.v1.Memberships;
using Shared.Contracts.Dtos.v1.Payments;

namespace Modules.Memberships.Application.Mappers;

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
    LangStr ToLangStr(string value);
}
