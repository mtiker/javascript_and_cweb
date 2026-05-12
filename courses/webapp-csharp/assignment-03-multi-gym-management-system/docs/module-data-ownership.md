# Module Data Ownership

**Companion to:** `docs/final2-module-plan.md`,
`docs/module-boundary-audit.md`, `docs/mediator-design.md`,
`docs/data-model.md`.

This document fixes the **data ownership** seam between Final-2 modules
**before** any entity is moved or any schema is split. It covers:

1. Which module owns each domain entity.
2. What stays shared/identity-only.
3. How cross-module reads happen at runtime (single DB now, optional
   per-module schema later).
4. What does NOT change in Phase 16.

Current evidence update, 2026-05-11:
- the ownership map is still logical, not physical schema isolation
- all modules still share one PostgreSQL database and one `AppDbContext`
- module-to-module project references are blocked by architecture tests
- Training category CRUD and MembershipFinance package CRUD are the strongest
  examples of module-owned handler workflows
- other mediated workflows may still use shared BLL services, so full module
  isolation is not claimed

---

## 1. Storage strategy in Phase 16

- **One PostgreSQL database, one `AppDbContext`.**
- All entities continue to live in their current `App.Domain/Entities/...`
  files. No tables are renamed. No migrations are added.
- "Ownership" is a **logical** boundary: the owning module is the only
  module whose code is allowed to write the entity's tables, and the only
  module whose code is allowed to read those tables directly. Other modules
  read via mediator queries (or via published read-model DTOs).
- Schema separation is **deferred** to Phase 21+ as an optional grade-defense
  story; if it lands, each module gets a Postgres schema (`users.*`,
  `gym.*`, `training.*`, `finance.*`) without renaming tables. Phase 16
  pre-locks ownership so that move is mechanical.

This matches the course's modular-monolith expectation: separate logical
modules, single host, single DB, defendable seams.

---

## 2. Entity → module map

Source: `src/App.Domain/Entities/`.

### Users

Identity, account auth, and the people-who-can-log-in surface.

| Entity | Notes |
|---|---|
| `AppUser` | inherits `IdentityUser<Guid>`. Owned by Users. |
| `AppRole` | inherits `IdentityRole<Guid>`. Owned by Users. |
| `AppUserGymRole` | maps user ↔ gym ↔ role. Owned by Users. |
| `RefreshToken` | rotation log per session. Owned by Users. |
| `Person` | name, DOB, address fragments. Owned by Users; `Member` and `Staff` reference by `PersonId`. |
| `Contact` | contact-method dictionary entries. Owned by Users. |
| `PersonContact` | join — owned by Users. |

### GymManagement

Tenant root, operations, staff, equipment, maintenance, audit.

| Entity | Notes |
|---|---|
| `Gym` | tenant root. Owned by GymManagement. Read by all modules through `GetGymSummaryQuery`. |
| `GymContact` | gym contact info. Owned here. |
| `GymSettings` | per-gym configuration. Owned here. |
| `OpeningHours` | weekly schedule. Owned here. |
| `OpeningHoursException` | one-off overrides. Owned here. |
| `Equipment` | equipment instance. Owned here. |
| `EquipmentModel` | reference catalogue. Owned here. |
| `MaintenanceTask` | maintenance work. Owned here. |
| `MaintenanceTaskAssignmentHistory` | audit-style history of task assignments. Owned here. |
| `Staff` | gym-employed staff person (FK → `Person`). Owned here. |
| `JobRole` | role catalogue. Owned here. |
| `EmploymentContract` | staff contract. Owned here. |
| `Vacation` | staff vacations. Owned here. |
| `WorkShift` | staff schedule slot. Owned here. (Read by Training: "is trainer X scheduled at time T?".) |
| `SupportTicket` | tenant support requests. Owned here. |
| `AuditLog` | append-only audit trail. Owned here; written by other modules through `WriteAuditLogCommand`. |

### Training

Training catalogue, sessions, bookings, coaching.

| Entity | Notes |
|---|---|
| `Member` | gym member (FK → `Person`, FK → `AppUser` for self-service). Owned by Training. |
| `TrainingCategory` | category catalogue per gym. Owned by Training. |
| `TrainingSession` | scheduled session. Owned by Training. |
| `Booking` | member-to-session booking. Owned by Training. |
| `CoachingPlan` | coaching plan header. Owned by Training. |
| `CoachingPlanItem` | coaching plan line items. Owned by Training. |

### MembershipFinance

Memberships, packages, payments, invoices, finance.

| Entity | Notes |
|---|---|
| `MembershipPackage` | sellable package. Owned here. |
| `Membership` | active membership instance for a member. Owned here. (Reads `Member` via mediator query.) |
| `Payment` | recorded payment. Owned here. |
| `Invoice` | invoice header. Owned here. |
| `InvoiceLine` | invoice line. Owned here. |
| `InvoicePayment` | payment ↔ invoice link. Owned here. |
| `Subscription` | tenant subscription tier. Owned here. (`SubscriptionTierLimitService` lives in GymManagement and reads via mediator query.) |

### Shared / not module-owned

These exist in `App.Domain` and are referenced by every module without
ownership conflict:

| Type | Path | Notes |
|---|---|---|
| `IBaseEntity`, `BaseEntity`, `TenantBaseEntity`, `ITenantEntity` | `App.Domain/Common/` | base abstractions. |
| `LangStr` | `App.Domain/Common/` | localized text value object. |
| `RoleNames` | `App.Domain/RoleNames.cs` | role name constants. |
| `Enums.*` | `App.Domain/Enums/` | enums consumed across modules. |
| `Identity` namespace | `App.Domain/Identity/` | base Identity types under Users; no per-module copy. |
| `Security` namespace | `App.Domain/Security/` | claim type constants. |

---

## 3. Foreign-key seams across modules

Cross-module FKs in the current schema (the seams that mediator queries cover
when modules can no longer touch each other's tables directly):

| FK | From module | To module | Cross-module access pattern |
|---|---|---|---|
| `Member.AppUserId` → `AppUser.Id` | Training | Users | Training reads identity profile via `GetUserContextQuery` / `GetAppUserSummaryQuery`. Writes via Users commands. |
| `Member.PersonId` → `Person.Id` | Training | Users | Training stores the FK; reads `Person` via published `PersonSummaryDto`. |
| `Staff.PersonId` → `Person.Id` | GymManagement | Users | Same pattern. |
| `Membership.MemberId` → `Member.Id` | MembershipFinance | Training | MembershipFinance reads `Member` via mediator query. |
| `Booking.MemberId` → `Member.Id` | Training (self) | — | Same module. |
| `TrainingSession.GymId` → `Gym.Id` | Training | GymManagement | Training reads gym summary via `GetGymSummaryQuery`. |
| `MembershipPackage.GymId` → `Gym.Id` | MembershipFinance | GymManagement | Same pattern. |
| `Subscription.GymId` → `Gym.Id` | MembershipFinance | GymManagement | Same. |
| `WorkShift.StaffId` → `Staff.Id` | GymManagement | GymManagement | Same module. |
| `MaintenanceTask.AssignedToStaffId` → `Staff.Id` | GymManagement | GymManagement | Same module. |
| `CoachingPlan.TrainerStaffId` → `Staff.Id` | Training | GymManagement | Training reads trainer summary via `GetStaffSummaryQuery`. |
| `Booking.PaymentId` → `Payment.Id` | Training | MembershipFinance | Training references the payment via mediator query (read-only); payment lifecycle owned by MembershipFinance. |

The FK constraints **stay in the database** in Phase 16 and through the
modular-monolith phases, because all entities still share `AppDbContext`.
The seam exists at the **code** level: only the owning module is allowed to
write/load the table directly. This gives Final-3 (microservice extraction)
a clean exit ramp — drop the FK at extraction time and switch to API/event
communication.

---

## 4. Tenant filter and soft delete

`AppDbContext` already applies query filters for `ITenantEntity` (`GymId`)
and soft delete for `TenantBaseEntity`. These filters are **shared
infrastructure** and remain in `App.DAL.EF`. Modules do not re-implement
them.

If a Phase 21 schema split happens, the EF model builder applies the same
filters per-entity regardless of schema; behavior is preserved.

---

## 5. Migrations and DbContext

- Phase 16: zero migrations added; `AppDbContext` and its configurations
  are unchanged.
- Future split (optional, Phase 21+): annotate entities with their schema
  via `modelBuilder.HasDefaultSchema(...)` per configuration grouping. No
  data movement, only schema rename. A single migration covers the change.
- `App.DAL.EF` remains the only project that knows about EF Core, regardless
  of how schemas are organized.

---

## 6. What does NOT change in Phase 16

- No entity moves to a module project.
- No `[Table(..., Schema = "...")]` annotations are added.
- No new DbContext is introduced.
- `AppDbContext` keeps `DbSet<>` properties for every entity.
- Repository contracts stay in `App.BLL.Contracts.Persistence` until the
  per-module slice migrates (Phase 17–20).

---

## 7. Defense one-liner

> *"Each entity has exactly one module owner; cross-module reads go through
> the mediator. The single database and single `AppDbContext` are an
> implementation detail, not a coupling — the seam is enforced by code,
> reviewed by an architecture test, and ready to be schema-split or
> extracted to a microservice in Final-3."*
