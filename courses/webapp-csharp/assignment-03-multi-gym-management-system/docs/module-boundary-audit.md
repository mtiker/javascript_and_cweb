# Module Boundary Audit (Final-1 → Final-2)

**Companion to:** `docs/final2-module-plan.md`,
`docs/module-data-ownership.md`, `docs/mediator-design.md`.

This audit takes the current Final-1 application services and DAL repository
contracts and assigns each one to a Final-2 module. It is the source of truth
for which existing class will live in which module assembly **once Phase 17+
moves the code**. Phase 16 itself does not move classes.

---

## 1. Modules at a glance

| Module | Bounded context | Lead use cases |
|---|---|---|
| **Users** | identity, account auth, role/gym assignment | login, refresh token rotation, register/manage app users, assign gym roles |
| **GymManagement** | tenant root and operations | gym CRUD, staff/contracts/vacations, equipment, maintenance, opening hours, gym settings, support tickets, audit log |
| **Training** | training catalog, sessions, bookings, coaching | members, training categories, training sessions, bookings, coaching plans |
| **MembershipFinance** | packages, memberships, payments, finance | membership packages, memberships, payments, invoices, refunds, finance workspace |

`BuildingBlocks` (not a module) carries the cross-module abstractions:
mediator, module marker, contract DTOs.

---

## 2. BLL services → modules

Sources: `src/App.BLL/Services/*.cs`,
`src/App.BLL/Contracts/Persistence/*.cs`, and slice plans under `docs/`.

### 2.1 Users

| Existing class | Path | Notes |
|---|---|---|
| `IAccountAuthService`, `AccountAuthService` | `App.BLL/Services/` | login, logout, refresh-token rotation. See `auth-service-boundary-audit.md`. |
| `IIdentityService`, `IdentityService` | `App.BLL/Services/` | registration, role/gym assignment, password reset. |
| `ITokenService`, `TokenService` | `App.BLL/Services/` | JWT issuance. |
| `IUserContextService`, `UserContextService` | `App.BLL/Services/` | active gym/role from claims. Will be exposed cross-module via `IUserContext` in BuildingBlocks. |
| `ICurrentActorResolver`, `CurrentActorResolver` | `App.BLL/Services/` | resolves the calling user/staff identity. |
| `IAuthorizationService`, `AuthorizationService`, `IResourceAuthorizationChecker`, `ResourceAuthorizationChecker`, `ITenantAccessChecker`, `TenantAccessChecker` | `App.BLL/Services/` | These are **cross-cutting**. They stay accessible to other modules via BuildingBlocks ports (e.g. `ITenantAccessChecker`) — implementation lives in Users. |
| `IRefreshTokenRepository`, `EfRefreshTokenRepository` | `App.BLL/Contracts/Persistence/`, `App.DAL.EF/Repositories/` | repository contract moves to Users module internal; EF impl stays in `App.DAL.EF`. |
| `IAuthResponseMapper`, `AuthResponseMapper` | `App.BLL/Mapping/` | maps tokens + tenant assignments into `JwtResponse`. |

Mediator surface (Users → consumers):
- query: `GetUserContextQuery` → `UserContextDto` (active gym, role flags) — replaces direct `IUserContextService` calls from other modules.
- query: `GetTenantAssignmentsQuery` → tenant assignment list for a user.
- query: `EnsureTenantAccessQuery` → 403/200 boolean for `(userId, gymId)`.
- command: `RotateRefreshTokenCommand`.

### 2.2 GymManagement

| Existing class | Path | Notes |
|---|---|---|
| `IPlatformService`, `PlatformService` | `App.BLL/Services/` | system-level gym registration, billing, snapshots, impersonation. |
| `IStaffWorkflowService`, `StaffWorkflowService` | `App.BLL/Services/` | staff, employment contracts, vacations, work shifts. WorkShift may stay shared with Training; see §3. |
| `IMaintenanceWorkflowService`, `MaintenanceWorkflowService` | `App.BLL/Services/` | equipment, equipment models, maintenance tasks, opening hours, opening-hours exceptions, gym settings, gym users. |
| `ISubscriptionTierLimitService`, `SubscriptionTierLimitService` | `App.BLL/Services/` | subscription tier checks. Keep here because it gates gym-level features. |
| `IMaintenanceRepository`, `EfMaintenanceRepository` | `App.BLL/Contracts/Persistence/`, `App.DAL.EF/Repositories/` | repo contract moves to GymManagement internal. |
| `IWorkShiftRepository`, `EfWorkShiftRepository` | `App.BLL/Contracts/Persistence/`, `App.DAL.EF/Repositories/` | repo contract: see §3 — currently scheduled with Training, may move to GymManagement. |
| `IMaintenanceMapper`, `MaintenanceMapper` | `App.BLL/Mapping/` | DTO mapping for maintenance/equipment. |

Mediator surface (GymManagement → consumers):
- query: `GetGymSummaryQuery` → gym name, time zone, subscription tier — used by Training/MembershipFinance for context.
- query: `EnsureGymActiveQuery` → boolean, gates write operations in other modules.

### 2.3 Training

| Existing class | Path | Notes |
|---|---|---|
| `IMemberWorkflowService`, `MemberWorkflowService` | `App.BLL/Services/` | member CRUD. See `member-repository-contract.md`. |
| `IMemberWorkspaceService`, `MemberWorkspaceService` | `App.BLL/Services/` | member-facing workspace queries. |
| `ITrainingWorkflowService`, `TrainingWorkflowService` | `App.BLL/Services/` | categories, sessions, bookings. |
| `IBookingPricingService`, `BookingPricingService` | `App.BLL/Services/` | booking price calculation. Reads `MembershipPackage` data via mediator query in Final-2. |
| `ICoachingPlanService`, `CoachingPlanService` | `App.BLL/Services/` | coaching plans + items. |
| `IMemberRepository`, `EfMemberRepository` | `App.BLL/Contracts/Persistence/`, `App.DAL.EF/Repositories/` | repo contract moves to Training internal. |
| `ITrainingCategoryRepository`, `EfTrainingCategoryRepository` | `App.BLL/Contracts/Persistence/`, `App.DAL.EF/Repositories/` | Training-internal. |
| `ITrainingSessionRepository`, `EfTrainingSessionRepository` | `App.BLL/Contracts/Persistence/`, `App.DAL.EF/Repositories/` | Training-internal. |
| `IBookingRepository`, `EfBookingRepository` | `App.BLL/Contracts/Persistence/`, `App.DAL.EF/Repositories/` | Training-internal. |
| `IMemberMapper`, `MemberMapper`, `ITrainingMapper`, `TrainingMapper` | `App.BLL/Mapping/` | DTO mappers. |

Mediator surface (Training → consumers):
- query: `GetMemberByUserIdQuery` → member lookup for finance.
- query: `IsMemberInGymQuery`.
- event: `BookingCreated`, `BookingCancelled` (later phase, used by finance).

### 2.4 MembershipFinance

| Existing class | Path | Notes |
|---|---|---|
| `List/Create/Update/DeleteMembershipPackage*Handler` | `Modules.MembershipFinance/Application/MembershipPackageHandlers.cs` | API package CRUD ownership, validation, tenant authorization, and UOW/repository persistence. |
| `IMembershipPackageService`, `MembershipPackageService` | `App.BLL/Services/` | package CRUD, validation rules. |
| `IMembershipService`, `MembershipService` | `App.BLL/Services/` | membership lifecycle. |
| `IMembershipWorkflowService`, `MembershipWorkflowService`, `MembershipWorkflowMapping` | `App.BLL/Services/` | extended membership operations. |
| `IPaymentService`, `PaymentService` | `App.BLL/Services/` | payments, refunds. |
| `IFinanceWorkspaceService`, `FinanceWorkspaceService` | `App.BLL/Services/` | finance workspace queries. |
| `IMembershipPackageRepository`, `IMembershipRepository`, `IPaymentRepository`, `IFinanceRepository` | `App.BLL/Contracts/Persistence/` | repo contracts: MembershipFinance-internal. |
| `IMembershipFinanceMapper`, `MembershipFinanceMapper` | `App.BLL/Mapping/` | DTO mapping. |

Mediator surface (MembershipFinance → consumers):
- query: `GetActiveMembershipQuery` → drives gating (used by Training when booking).
- query: `CheckSubscriptionAllowsActionQuery`.
- event: `MembershipActivated`, `PaymentCompleted` (later phase).

---

## 3. Disputed seams (call out before moving code)

| Seam | Decision | Rationale |
|---|---|---|
| `Member` vs `AppUser` | `Member` belongs to **Training**, `AppUser` belongs to **Users**. | `Member` exists for booking/coaching context. Identity is in Users. Training reads `AppUser` profile via mediator. |
| `WorkShift` (staff schedule) | Lives in **GymManagement** (staff scheduling). Training reads "is trainer working?" via mediator. | Staff scheduling is a gym-operations concern, not a training concern. |
| `Subscription` (tenant tier) vs `Membership` (member-level) | `Subscription` (tenant tier) → **GymManagement** seam consumer; entity owned by **MembershipFinance** with a published query. | The two were conflated historically; this audit pins the split before any move. |
| `Person` and `Contact` | **Users** owns `Person`/`Contact`/`PersonContact` (general identity). `Member`/`Staff` reference `PersonId` only. | Avoids duplicating identity rows per module. |
| `AuditLog` | **GymManagement** owns it. All modules append via a mediator command. | Single tenant-wide audit trail. |
| `SupportTicket` | **GymManagement** owns it. | Tenant operations. |

---

## 4. Controllers and presentation

ApiControllers live in `WebApp` and remain the composition root surface. They
keep depending on application service interfaces. As each phase migrates a
slice, controller constructors switch from "direct service" to either "module
service" or "`IMediator.SendAsync`" depending on the integration shape.
Controller routes do not change.

| Controller | Module backing service | Phase to migrate |
|---|---|---|
| `AccountController` | Users — `IAccountAuthService`, `IIdentityService` | 17 |
| `MembersController` | Training — `IMemberWorkflowService` | 18 |
| `TrainingCategoriesController`, `TrainingSessionsController`, `BookingsController`, `WorkShiftsController` | Training (WorkShifts goes to GymManagement) | 18 + 20 |
| `MembershipPackagesController`, `MembershipsController`, `PaymentsController`, `FinanceWorkspaceController` | MembershipFinance | 19 |
| `EquipmentController`, `EquipmentModelsController`, `MaintenanceTasksController`, `OpeningHoursController`, `OpeningHoursExceptionsController`, `GymSettingsController`, `GymUsersController` | GymManagement | 20 |
| Platform/system controllers | GymManagement (platform sub-area) | 20 |
| MVC Admin and MVC Client controllers | served by the same module services | follows API phase |

---

## 5. Cross-cutting concerns

| Concern | Owner | Exposed to other modules via |
|---|---|---|
| Tenant context (active gym, active role) | Users | `IUserContext` port in BuildingBlocks; `GetUserContextQuery` mediator query |
| Authorization helpers | Users | `ITenantAccessChecker`, `IResourceAuthorizationChecker` ports in BuildingBlocks |
| Localization (`LangStr`) | App.Domain (already shared) | direct type reuse, no module ownership |
| Audit logging | GymManagement | `WriteAuditLogCommand` mediator command |
| Subscription tier limits | GymManagement | `EnsureTierAllowsActionQuery` mediator query |
| Clock | BuildingBlocks (`IClock`) | DI singleton |

---

## 6. What stays in `App.BLL` after Phase 21

Final goal: `App.BLL` shrinks to **infrastructure ports only** that aren't
module-owned:
- `IAppUnitOfWork` and per-module repository contracts (or these move into
  modules in Phase 21 once stable).
- Mapping marker types if they remain shared.
- The legacy `IAppDbContext` port disappears once all modules are off it
  (already targeted by Final-1 Phase 13).

If Phase 21 still leaves anything in `App.BLL` that nobody outside one module
uses, it gets moved into that module and `App.BLL` is collapsed.

---

## 7. Audit results — Phase 16 baseline

- ✅ Every BLL service has an assigned module owner.
- ✅ Every repository contract has an assigned module owner.
- ✅ Every entity has an assigned module owner (see
  `docs/module-data-ownership.md`).
- ✅ Cross-module mediator surface is sketched per module (request types not
  yet created in code; created when the corresponding slice migrates).
- ✅ Disputed seams are explicitly resolved.
- 🔒 No code has been moved yet. This audit is the contract that Phase 17+
  will execute.
