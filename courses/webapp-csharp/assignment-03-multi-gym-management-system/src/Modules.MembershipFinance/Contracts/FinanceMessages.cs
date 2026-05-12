using App.DTO.v1.Finance;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;
using BuildingBlocks.Mediator;

namespace Modules.MembershipFinance.Contracts;

public sealed record ListMembershipPackagesQuery(string GymCode) : IRequest<IReadOnlyCollection<MembershipPackageResponse>>;

public sealed record CreateMembershipPackageCommand(string GymCode, MembershipPackageUpsertRequest Request) : IRequest<MembershipPackageResponse>;

public sealed record UpdateMembershipPackageCommand(string GymCode, Guid PackageId, MembershipPackageUpsertRequest Request) : IRequest<MembershipPackageResponse>;

public sealed record DeleteMembershipPackageCommand(string GymCode, Guid PackageId) : IRequest;

public sealed record ListMembershipsQuery(string GymCode) : IRequest<IReadOnlyCollection<MembershipResponse>>;

public sealed record SellMembershipCommand(string GymCode, SellMembershipRequest Request) : IRequest<MembershipSaleResponse>;

public sealed record UpdateMembershipStatusCommand(string GymCode, Guid MembershipId, MembershipStatusUpdateRequest Request) : IRequest<MembershipResponse>;

public sealed record DeleteMembershipCommand(string GymCode, Guid MembershipId) : IRequest;

public sealed record ListPaymentsQuery(string GymCode) : IRequest<IReadOnlyCollection<PaymentResponse>>;

public sealed record CreatePaymentCommand(string GymCode, PaymentCreateRequest Request) : IRequest<PaymentResponse>;

public sealed record GetCurrentFinanceWorkspaceQuery(string GymCode) : IRequest<FinanceWorkspaceResponse>;

public sealed record GetMemberFinanceWorkspaceQuery(string GymCode, Guid MemberId) : IRequest<FinanceWorkspaceResponse>;

public sealed record ListInvoicesQuery(string GymCode, Guid? MemberId) : IRequest<IReadOnlyCollection<InvoiceResponse>>;

public sealed record GetInvoiceQuery(string GymCode, Guid InvoiceId) : IRequest<InvoiceResponse>;

public sealed record CreateInvoiceCommand(string GymCode, InvoiceCreateRequest Request) : IRequest<InvoiceResponse>;

public sealed record PostInvoicePaymentCommand(string GymCode, Guid InvoiceId, InvoicePaymentRequest Request) : IRequest<InvoiceResponse>;

public sealed record PostInvoiceRefundCommand(string GymCode, Guid InvoiceId, InvoicePaymentRequest Request) : IRequest<InvoiceResponse>;
