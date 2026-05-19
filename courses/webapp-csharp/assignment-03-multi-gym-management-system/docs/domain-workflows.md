# Domain Workflows

This document keeps the business rules in one place. Update it when behavior,
validation, state transitions, or user-facing workflow scope changes.

## Platform SaaS

Main workflows:
- gym onboarding
- gym activation/deactivation
- platform analytics

Rules:
- `SystemAdmin` owns onboarding, activation, snapshots, and analytics
- tenant roles do not access `/api/v1/system/...`

## Members

Surfaces:
- REST API member CRUD
- MVC Admin member CRUD
- React members page
- member workspace

Rules:
- member rows are tenant-owned through `GymId`
- gym admins/owners can create, update, and delete members in the active gym
- cross-gym member IDs are denied
- member self routes return only the signed-in member's own profile
- duplicate member validation returns API `ProblemDetails`
- member list and detail responses are separate shapes; detail includes fields
  needed by edit forms

Development notes:
- member APIs currently route through GymManagement mediator messages
- deeper member workflow logic still lives in shared BLL and should move into
  GymManagement module handlers during Final2 hardening

## Training

Surfaces:
- training category CRUD
- training session list/detail/create/update/delete
- bookings
- attendance updates

Rules:
- training categories use `LangStr` for DB-backed translations
- React and API requests send `Accept-Language` for localized values
- training session descriptions may be nullable and must render safely
- booking creation rejects duplicate member/session bookings
- booking creation requires a payment reference when payment is due
- trainers can update attendance only for assigned sessions
- trainer assignment uses optional `TrainerStaffId`

Development notes:
- Training category CRUD is already module-owned in
  `Modules.Training.Application.TrainingCategoryHandlers`
- sessions, bookings, and attendance are mediated
  but still delegate to shared BLL services
- next Final2 step is moving those handlers to direct UOW/repository usage

## Membership And Finance

Surfaces:
- membership package CRUD
- membership sale and status changes
- payments
- outstanding balance in member workspace

Membership package rules:
- package price must be valid and non-negative
- duration must be valid
- currency is required
- unused packages can be soft-deleted
- used packages return conflict on delete to preserve membership snapshots
- package text can be localized where applicable

Membership lifecycle states:
- `Pending`
- `Active`
- `Paused`
- `Expired`
- `Cancelled`
- `Refunded`
- `Renewed`

Finance rules:
- payments reduce outstanding balance
- payment records are internal ledger records only; no external payment
  provider is integrated

Development notes:
- Membership package CRUD is already module-owned in
  `Modules.MembershipFinance.Application.MembershipPackageHandlers`
- membership sale/status/delete and payments are mediated but still partly transitional
- next Final2 step is moving those broader workflows into
  MembershipFinance module handlers

## Maintenance And Facilities

Surfaces:
- equipment models
- equipment
- maintenance tasks
- due-task generation
- caretaker maintenance workspace

Rules:
- equipment and maintenance rows are tenant-owned
- maintenance task status updates preserve assignment and workflow constraints
- recurring due-task generation creates due tasks from equipment intervals
- assignment changes update the current assignee and optional task note
- completion notes and equipment downtime fields are part of the workflow
- caretakers can update only assigned tasks
- gym admins/owners can manage broader maintenance data

Development notes:
- maintenance/equipment endpoints are mediated through GymManagement contracts
- most handlers still delegate to `IMaintenanceWorkflowService`
- move these handlers into module-owned GymManagement application code after
  member workflow migration

## Staff And Operations

Surfaces:
- staff

Rules:
- staff rows are tenant-owned
- staff can be referenced by trainer assignment and maintenance assignment

Development notes:
- `StaffWorkflowService` still uses `IAppDbContext`
- migrating staff to repository contracts and GymManagement module handlers is
  a high-value Final1/Final2 cleanup

## MVC Admin Scope

Current full CRUD areas:
- members
- training categories
- membership packages

Current read/action areas:
- dashboard
- gyms
- memberships
- sessions
- operations

Defense wording:

"MVC Admin demonstrates real Razor pages with focused tested CRUD for three
tenant entities and read/action dashboards for the defended Final2 domain. The
React client and REST API expose the same focused gym-operations workflows."

If more time is available, add one additional MVC Admin mutation workflow:
1. membership sale/status action, because it demonstrates MembershipFinance
2. equipment CRUD, because it demonstrates GymManagement operations
3. equipment CRUD, because it is already inside the defended operations scope

## React Client Scope

Focused pages:
- login/logout
- members CRUD
- training categories CRUD
- membership packages CRUD
- sessions list/detail and booking
- attendance
- maintenance
- member workspace

Rules:
- one active gym context at a time
- assigned multi-gym users can switch active gym/role
- SystemAdmin can pick tenant context for tenant routes
- API calls include bearer token and selected `Accept-Language`
- one access-token refresh retry is attempted after `401`
- refresh failure clears session state

## Validation Pointers

Backend tests:
- `Final1CriticalE2ETests`
- `AdminMembersCrudTests`
- `AdminTrainingCategoriesCrudTests`
- `AdminMembershipPackagesCrudTests`
- `MemberCrudTests`
- `MembershipPackageCrudTests`
- `MembershipWorkflowServiceTests`
- `MaintenanceWorkflowServiceTests`
- `StaffWorkflowTests`
- `ProposalWorkflowTests`
- `TrainingModuleMediatorTests`
- `MembershipFinanceModuleMediatorTests`
- `MaintenanceModuleMediatorTests`

Frontend tests:
- `CrudPages.test.tsx`
- `WorkspacePages.test.tsx`
- `OperationsPages.test.tsx`
- `SessionsPage.test.tsx`
- `App.test.tsx`

Manual smoke checks live in [testing.md](testing.md).
