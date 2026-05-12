# Finance Mediator Messages

MembershipFinance mediator contracts live in
`src/Modules.MembershipFinance/Contracts/FinanceMessages.cs`. Package handlers
live in `src/Modules.MembershipFinance/Application/MembershipPackageHandlers.cs`;
the remaining finance handlers live in
`src/Modules.MembershipFinance/Application/FinanceHandlers.cs`.

## Flow

```text
WebApp controller
  -> IMediator.SendAsync(message)
  -> Modules.MembershipFinance.Application handler
  -> package workflow in the module OR transitional workflow service
```

Package list/create/update/delete is owned by module handlers directly. Those
handlers use the existing UOW, authorization, repository, and mapper contracts
without wrapping `IMembershipWorkflowService` or `IMembershipPackageService`.
Membership, payment, invoice, refund, and workspace messages still delegate to
the existing BLL workflow services in this phase.

## Package Messages

| Message | Response | Purpose |
|---|---|---|
| `ListMembershipPackagesQuery(string GymCode)` | `IReadOnlyCollection<MembershipPackageResponse>` | List tenant packages. |
| `CreateMembershipPackageCommand(string GymCode, MembershipPackageUpsertRequest Request)` | `MembershipPackageResponse` | Create a package. |
| `UpdateMembershipPackageCommand(string GymCode, Guid PackageId, MembershipPackageUpsertRequest Request)` | `MembershipPackageResponse` | Update a package. |
| `DeleteMembershipPackageCommand(string GymCode, Guid PackageId)` | none | Delete a package. |

## Membership Messages

| Message | Response | Purpose |
|---|---|---|
| `ListMembershipsQuery(string GymCode)` | `IReadOnlyCollection<MembershipResponse>` | List tenant memberships. |
| `SellMembershipCommand(string GymCode, SellMembershipRequest Request)` | `MembershipSaleResponse` | Sell a membership and create the sale result. |
| `UpdateMembershipStatusCommand(string GymCode, Guid MembershipId, MembershipStatusUpdateRequest Request)` | `MembershipResponse` | Apply membership lifecycle transition rules. |
| `DeleteMembershipCommand(string GymCode, Guid MembershipId)` | none | Delete a membership. |

## Payment And Invoice Messages

| Message | Response | Purpose |
|---|---|---|
| `ListPaymentsQuery(string GymCode)` | `IReadOnlyCollection<PaymentResponse>` | List tenant payments. |
| `CreatePaymentCommand(string GymCode, PaymentCreateRequest Request)` | `PaymentResponse` | Post a membership or booking payment. |
| `ListInvoicesQuery(string GymCode, Guid? MemberId)` | `IReadOnlyCollection<InvoiceResponse>` | List invoices, optionally for one member. |
| `GetInvoiceQuery(string GymCode, Guid InvoiceId)` | `InvoiceResponse` | Load one invoice. |
| `CreateInvoiceCommand(string GymCode, InvoiceCreateRequest Request)` | `InvoiceResponse` | Create an invoice with lines and credits. |
| `PostInvoicePaymentCommand(string GymCode, Guid InvoiceId, InvoicePaymentRequest Request)` | `InvoiceResponse` | Post a payment ledger entry. |
| `PostInvoiceRefundCommand(string GymCode, Guid InvoiceId, InvoicePaymentRequest Request)` | `InvoiceResponse` | Post a refund/credit ledger entry. |

## Workspace Messages

| Message | Response | Purpose |
|---|---|---|
| `GetCurrentFinanceWorkspaceQuery(string GymCode)` | `FinanceWorkspaceResponse` | Load the current member/admin finance workspace. |
| `GetMemberFinanceWorkspaceQuery(string GymCode, Guid MemberId)` | `FinanceWorkspaceResponse` | Load a specific member finance workspace, preserving self-access rules. |

## Registration

`AddMembershipFinanceModule` scans the MembershipFinance assembly for
`IRequestHandler<,>` and `IRequestHandler<>` implementations. WebApp knows the
module only at composition root level.
