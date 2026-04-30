# Membership Repository Contract

Date: 2026-04-30

## Purpose

Membership and finance services must not depend on EF Core or `IAppDbContext` directly. The BLL owns persistence contracts, and `App.DAL.EF` owns EF query details.

## Unit Of Work Surface

`IAppUnitOfWork` exposes:

- `MembershipPackages`
- `Memberships`
- `Payments`
- `Finance`

All write use cases commit through `IAppUnitOfWork.SaveChangesAsync`.

## Repository Contracts

`IMembershipPackageRepository`:
- list packages by gym
- find package by `(gymId, packageId)`
- check whether a package is used by memberships
- add and remove packages

`IMembershipRepository`:
- list memberships by gym or member
- list active memberships with member/package details for MVC admin summaries
- find membership by `(gymId, membershipId)`
- detect overlaps and previous memberships
- list member membership IDs for payment filtering
- add and remove memberships

`IPaymentRepository`:
- list payments by gym
- list payments linked to a member's memberships or bookings
- add internal payment records

`IFinanceRepository`:
- list invoices by gym and optional member
- load one invoice with member, lines, and payment ledger rows
- count same-day invoices for invoice numbering
- add invoices, payments, and invoice-payment ledger rows

## Tenant Rule

Every repository lookup that returns business data accepts `gymId` and includes it in the query. Services still perform authorization first; repositories make accidental cross-tenant reads harder.

## Delete Rule

Unused packages can be soft-deleted through the existing `TenantBaseEntity` soft-delete path. Used packages return `409 Conflict` from the API because deleting a package already referenced by memberships would make package lifecycle semantics ambiguous. Historical memberships keep `PriceAtPurchase` and `CurrencyCode` snapshots.
