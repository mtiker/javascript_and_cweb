# Study Guide: Domain Layer

## Purpose
The domain layer (`src/App.Domain`) defines business entities, enums, and security claim constants used across the whole SaaS platform.

## Core Design Rules
- Every business entity has a `GymId` when it is tenant-owned.
- Soft-delete and audit-capable entities derive from shared base abstractions in `Common`.
- Domain enums are the canonical state vocabulary used by BLL, DTO mapping, and tests.

## Platform-Centric Entities
- `Gym`, `Subscription`, `SupportTicket`, `AuditLog`
- `AppUser`, `AppRole`, `AppRefreshToken`, `AppUserGymRole`

## Tenant-Centric Entities
- Members and identity context: `Member`, `Staff`, `JobRole`, `EmploymentContract`, `Vacation`
- Training workflows: `TrainingCategory`, `TrainingSession`, `WorkShift`, `Booking`
- Membership and finance workflows: `MembershipPackage`, `Membership`, `Payment`, `Invoice`, `InvoiceLine`, `InvoicePayment`
- Operational workflows: `EquipmentModel`, `Equipment`, `MaintenanceTask`, `MaintenanceTaskAssignmentHistory`
- Coaching workflows: `CoachingPlan`, `CoachingPlanItem`

## Key State Machines
- Membership lifecycle: `Pending -> Active -> Paused/Expired/Cancelled/Refunded/Renewed`
- Coaching plan lifecycle: `Draft -> Published -> Active -> Completed` (or `Cancelled`)
- Invoice lifecycle: `Draft/Issued -> PartiallyPaid/Paid/Overdue` (plus `Cancelled/Refunded`)

## Defense Notes
- The domain intentionally uses gym terminology only (no dental vocabulary).
- Tenant isolation starts in the model shape itself through required tenant ownership keys.
