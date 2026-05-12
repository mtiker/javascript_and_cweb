# Final-2 MembershipFinance Module Plan

**Status:** Phase 20 implemented as a mediated HTTP adapter slice; membership package API CRUD is now module-owned internally.

## Scope

This phase moves the remaining membership and finance tenant API adapters
behind `Modules.MembershipFinance` mediator messages while preserving existing
routes, DTOs, status codes, and validation/error behavior.

Covered workflows:

- membership package CRUD
- membership list, sale, status transition, and delete
- payment list and posting
- finance workspace reads
- invoice list/detail/create
- invoice payment posting
- invoice refund/credit posting
- outstanding balance reads through the finance workspace

No external payment provider is introduced. Payment and refund posting remain
internal ledger operations backed by the existing domain entities.

## Implemented Shape

```text
HTTP request
  -> WebApp finance/membership controller
  -> IMediator.SendAsync(...)
  -> Modules.MembershipFinance.Application handler
  -> package handler uses UOW/auth/mapper directly
     OR remaining membership/payment/invoice handler uses existing workflow service
  -> repository/unit-of-work boundary
```

Membership package list/create/update/delete is owned by
`Modules.MembershipFinance.Application` handlers and no longer delegates through
`IMembershipWorkflowService` or `IMembershipPackageService` on the API path.
The remaining membership, payment, invoice, refund, and workspace workflows are
still adapter migrations over the existing BLL services so their tested behavior
stays stable in this phase.

## Public API Contract

The following routes keep their existing contract:

- `GET/POST/PUT/DELETE /api/v1/{gymCode}/membership-packages`
- `GET/POST/DELETE /api/v1/{gymCode}/memberships`
- `PUT /api/v1/{gymCode}/memberships/{id}/status`
- `GET/POST /api/v1/{gymCode}/payments`
- `GET /api/v1/{gymCode}/finance-workspace/me`
- `GET /api/v1/{gymCode}/finance-workspace/members/{memberId}`
- `GET/POST /api/v1/{gymCode}/invoices`
- `GET /api/v1/{gymCode}/invoices/{id}`
- `POST /api/v1/{gymCode}/invoices/{id}/payments`
- `POST /api/v1/{gymCode}/invoices/{id}/refunds`

## Cross-Module Lookups

MembershipFinance still uses the existing workflow services internally for
memberships, payments, invoices, refunds, and finance workspace behavior. Member
self-access and member finance lookup checks are reached through mediated
finance workspace messages (`GetCurrentFinanceWorkspaceQuery` and
`GetMemberFinanceWorkspaceQuery`) instead of direct controller-to-service
calls. The next extraction step can replace the BLL-internal member lookup with
a Training-owned mediator query without changing the HTTP adapter contract.

## Tests

Coverage for this phase:

- `MembershipFinanceModuleMediatorTests`
- `MembershipFinanceCleanSliceTests`
- `MembershipWorkflowServiceTests`
- `TenantControllerTests`
- `AdditionalControllerTests`
- `ArchitectureTests`
- `ModuleArchitectureTests`

The tests cover module-owned package CRUD and boundary ownership, membership
status transitions, invoice creation, payment posting, refund/credit posting,
outstanding balance calculation, and the no-direct-module-reference rule.

## Remaining Work

- Move the remaining MembershipFinance business services and mapper internals
  from `App.BLL` into `Modules.MembershipFinance.Application`.
- Replace BLL-internal member reads with explicit Training-owned lookup
  messages when the Training member lookup surface is extracted.
- Add integration events only if later phases need asynchronous finance
  notifications.
