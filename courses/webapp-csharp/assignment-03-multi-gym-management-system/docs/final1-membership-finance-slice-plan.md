# Final1 Membership And Finance Slice Plan

Date: 2026-04-30
Phase: 13 - Final1 membership and finance clean slice

## Course Context

Personal Project Final1 requires the ASP.NET Core application to use Clean/Onion architecture with REST API controllers, MVC UX, JWT auth, repositories, Unit of Work, services/BLL, mappers, and mandatory tests. This slice migrates membership packages, memberships, payments, invoices, refunds, and the finance workspace without adding an external payment provider.

## Implemented Boundary

| Concern | Final location |
| --- | --- |
| Membership package use cases | `src/App.BLL/Services/MembershipPackageService.cs` |
| Membership sale/status use cases | `src/App.BLL/Services/MembershipService.cs` |
| Payment posting use cases | `src/App.BLL/Services/PaymentService.cs` |
| Invoice/refund/workspace use cases | `src/App.BLL/Services/FinanceWorkspaceService.cs` |
| Persistence contracts | `src/App.BLL/Contracts/Persistence/*Membership*`, `IPaymentRepository`, `IFinanceRepository` |
| EF implementations | `src/App.DAL.EF/Repositories/EfMembership*`, `EfPaymentRepository`, `EfFinanceRepository` |
| Unit of Work access | `IAppUnitOfWork.MembershipPackages`, `Memberships`, `Payments`, `Finance` |
| Entity-to-DTO mapping | `src/App.BLL/Mapping/MembershipFinanceMapper.cs` |
| API adapters | `src/WebApp/ApiControllers/Tenant/*Membership*`, `PaymentsController`, `FinanceController` |
| MVC adapter | `src/WebApp/Areas/Admin/Controllers/MembershipsController.cs` |

## Request Flow

Package CRUD:
1. Controller delegates to `IMembershipWorkflowService`.
2. Workflow delegates to `IMembershipPackageService`.
3. Service authorizes tenant/admin role, validates input, then uses `IMembershipPackageRepository`.
4. `MembershipFinanceMapper` maps entity responses.

Membership sale/status:
1. Service validates member and package through tenant-scoped repositories.
2. Overlap detection and status-transition rules stay in BLL.
3. Membership sale records a completed internal `Payment` when package price is positive.

Finance:
1. Invoice creation validates member ownership, lines, credits, totals, and invoice number.
2. Payment and refund posting create internal ledger rows only.
3. Outstanding balance is derived from invoice totals and invoice payments/refunds.
4. Member finance workspace access remains protected through `EnsureMemberSelfAccessAsync`.

## Tests

Added or updated coverage:
- membership package CRUD
- package validation
- used package delete conflict
- membership status transitions
- invoice creation
- payment posting
- refund/credit posting
- outstanding balance calculation
- member finance workspace access rules
- architecture boundary tests for repositories, Unit of Work, mapper, and no service `IAppDbContext` dependency

Validation command:

```powershell
dotnet test .\multi-gym-management-system.slnx
```

Result on 2026-04-30: 159 passed, 3 skipped Docker-gated PostgreSQL tests.

## Out Of Scope

- External payment provider integration.
- Payment gateway callbacks, webhooks, settlement, or card token handling.
- Changing public route paths or existing public DTO shapes except adding internal MVC summary DTO support.
- Modular monolith module extraction.
