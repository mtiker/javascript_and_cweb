# Maintenance Rules Audit

## Scope

This audit covers maintenance tasks, assignment history, due scheduled-task
generation, and equipment downtime/status behavior in the Final1 maintenance
slice.

## Rules

| Rule | Implementation | Verification |
|---|---|---|
| Caretakers can update only assigned tasks | `ResourceAuthorizationChecker.EnsureMaintenanceTaskAccessAsync` checks current caretaker staff against `MaintenanceTask.AssignedStaffId` | `UpdateTaskStatusAsync_AllowsAssignedCaretaker`, `UpdateTaskStatusAsync_RejectsUnassignedCaretaker` |
| Gym owner/admin can assign tasks | `MaintenanceWorkflowService.UpdateTaskAssignmentAsync` requires `GymOwner` or `GymAdmin` | Assignment-history unit test and existing tenant access tests |
| Assignment changes are auditable | `MaintenanceTaskAssignmentHistory` row is added on task creation and assignment update | `UpdateTaskAssignmentAsync_AppendsAssignmentHistory` |
| Due scheduled tasks are generated only when due | generation compares latest completed scheduled task or commissioning date plus model interval against current UTC date | `GenerateDueScheduledTasksAsync_CreatesOneOpenScheduledTaskPerDueEquipment` |
| Existing open scheduled task prevents duplicates | repository checks open non-done scheduled task before creating another | same due-generation test calls generation twice |
| Decommissioned equipment is skipped | due candidate query excludes `EquipmentStatus.Decommissioned` | covered by repository predicate; no dedicated test added in this slice |
| Breakdown in progress starts downtime | `InProgress` on breakdown sets `StartedAtUtc`, `DowntimeStartedAtUtc`, and moves active equipment to maintenance | `BreakdownStatusUpdates_MoveEquipmentIntoAndOutOfMaintenance` |
| Breakdown done ends downtime | `Done` sets completion/downtime end and moves maintenance equipment back to active | same downtime test |

## Boundary Findings

- Maintenance/facility persistence is behind `IMaintenanceRepository`.
- DTO projection is centralized in `MaintenanceMapper`.
- `MaintenanceWorkflowService` no longer depends on `IAppDbContext`.
- Tenant predicates are explicit in repository methods, including task, history,
  equipment, opening-hours, settings, and gym-user role lookups.

## Remaining Risk

- No dedicated test currently asserts decommissioned-equipment due-generation
  skip behavior; the rule is present in the repository predicate and should be
  covered if that path changes.

