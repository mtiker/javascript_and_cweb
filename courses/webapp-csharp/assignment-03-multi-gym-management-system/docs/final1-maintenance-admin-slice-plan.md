# Final1 Maintenance/Admin Slice Plan

## Status

Phase 14 completes the Final1 maintenance, facilities, platform-role, and MVC
Admin cleanup slice.

The public API and MVC route surface is unchanged. No new SaaS features were
added.

## Course Context

This slice supports the Assignment 03 Final1 defense requirements:

- controllers stay boundary adapters
- application rules live in BLL services
- EF-specific queries live behind BLL persistence contracts
- entity-to-DTO mapping lives in BLL mappers
- Admin MVC pages render strongly typed view models without `ViewBag` or
  `ViewData`
- platform routes remain role-gated to system roles

## Implemented Boundary

| Concern | Location |
|---|---|
| Maintenance/facility use cases | `src/App.BLL/Services/MaintenanceWorkflowService.cs` |
| Maintenance persistence contract | `src/App.BLL/Contracts/Persistence/IMaintenanceRepository.cs` |
| EF maintenance persistence | `src/App.DAL.EF/Repositories/EfMaintenanceRepository.cs` |
| Unit of Work access | `IAppUnitOfWork.Maintenance` |
| Maintenance/facility mapping | `src/App.BLL/Mapping/IMaintenanceMapper.cs`, `MaintenanceMapper.cs` |
| Admin page composition | `src/WebApp/Areas/Admin/Services/AdminViewModelServices.cs` |
| Admin controllers | thin request/authorization adapters under `src/WebApp/Areas/Admin/Controllers` |

## Request Flow

Maintenance status update:

1. Tenant route and role are validated with
   `IAuthorizationService.EnsureTenantAccessAsync`.
2. `IMaintenanceRepository.FindMaintenanceTaskAggregateAsync` loads the task
   aggregate with equipment, assigned staff, and assignment history.
3. `EnsureMaintenanceTaskAccessAsync` enforces caretaker assignment rules.
4. `MaintenanceWorkflowService` applies status and downtime transitions.
5. `IAppUnitOfWork.SaveChangesAsync` persists the change.
6. `IMaintenanceMapper` projects the entity graph to `MaintenanceTaskResponse`.

Admin MVC page rendering:

1. Admin controller checks the active role and route context.
2. Controller delegates page data composition to an Admin page service.
3. Page service returns a concrete `Admin*ViewModel`.
4. Razor view renders only the typed model and does not use dynamic view data.

## Tests

Added or extended:

- `MaintenanceWorkflowServiceTests.UpdateTaskStatusAsync_AllowsAssignedCaretaker`
- `MaintenanceWorkflowServiceTests.UpdateTaskStatusAsync_RejectsUnassignedCaretaker`
- `MaintenanceWorkflowServiceTests.GenerateDueScheduledTasksAsync_CreatesOneOpenScheduledTaskPerDueEquipment`
- `MaintenanceWorkflowServiceTests.UpdateTaskAssignmentAsync_AppendsAssignmentHistory`
- `MaintenanceWorkflowServiceTests.BreakdownStatusUpdates_MoveEquipmentIntoAndOutOfMaintenance`
- `ArchitectureTests.MaintenanceSlice_UsesDedicatedRepositoryAndMapperBoundaries`
- `ArchitectureTests.AdminMvcControllers_AreThinAndDoNotDependOnDbContext`
- `MvcComplianceTests.AdminViews_RenderOnlyStronglyTypedViewModels`
- `AuthSecurityAndErrorTests.SystemPlatformAnalytics_AllowsPlatformRoles`

## Out Of Scope

- New maintenance domain features.
- New SaaS modules.
- New MVC write forms for Admin pages.
- Client React feature changes.
- Platform service repository migration beyond the existing service boundary.

