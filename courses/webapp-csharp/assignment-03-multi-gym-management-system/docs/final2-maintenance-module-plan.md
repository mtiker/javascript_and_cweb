# Final-2 Maintenance Module Plan

**Status:** Phase 20 implemented as a mediated maintenance adapter slice inside
`Modules.GymManagement`.

## Scope

Maintenance is logically owned by GymManagement in the current module data
ownership map because equipment, equipment models, opening hours, settings,
gym users, staff, and maintenance tasks belong to the same operational bounded
context. This document tracks the maintenance slice separately because the
HTTP workflow and defense story are large enough to need their own plan.

Covered workflows:

- equipment model CRUD
- equipment CRUD
- opening hours CRUD
- opening-hours exception CRUD
- maintenance task list/create/status/assignment/history/delete
- caretaker assigned-task status update
- caretaker forbidden path for unassigned tasks
- due scheduled maintenance task generation
- gym settings read/update
- gym user role assignment/removal

## Implemented Shape

```text
HTTP request
  -> WebApp maintenance/facility controller
  -> IMediator.SendAsync(...)
  -> Modules.GymManagement.Application.Maintenance handler
  -> existing maintenance workflow service
  -> repository/unit-of-work boundary
```

The route and DTO contract is preserved. The existing BLL workflow remains the
source of maintenance authorization and business rules during this adapter
phase.

## Public API Contract

The following route groups remain unchanged:

- `/api/v1/{gymCode}/equipment-models`
- `/api/v1/{gymCode}/equipment`
- `/api/v1/{gymCode}/opening-hours`
- `/api/v1/{gymCode}/opening-hours-exceptions`
- `/api/v1/{gymCode}/maintenance-tasks`
- `/api/v1/{gymCode}/gym-settings`
- `/api/v1/{gymCode}/gym-users`

## Caretaker Lookup Boundary

Caretaker assignment checks are preserved by dispatching task status updates
through `UpdateMaintenanceTaskStatusCommand`. The handler calls the existing
maintenance workflow, which uses the current actor resolver and resource
authorization checker to map the authenticated user to staff/caretaker context
and reject unassigned tasks.

## Tests

Coverage for this phase:

- `MaintenanceModuleMediatorTests`
- `MaintenanceWorkflowServiceTests`
- `AdditionalControllerTests`
- `ArchitectureTests`
- `ModuleArchitectureTests`

The tests cover maintenance task list dispatch, assigned caretaker update,
forbidden unassigned caretaker update, due-task generation, and no direct
module references.

## Remaining Work

- Move maintenance workflow internals from `App.BLL` into
  `Modules.GymManagement.Application.Maintenance`.
- Publish staff/caretaker lookup messages if Training or other modules need
  staff context without direct GymManagement data access.
- Split GymManagement operational handlers into smaller files if handler count
  becomes hard to navigate.
