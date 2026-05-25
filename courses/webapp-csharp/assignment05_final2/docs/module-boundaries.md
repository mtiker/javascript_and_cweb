# Final2 Module Architecture Status

Final2 is refactoring the defended multi-gym system into a modular monolith.
The current implementation is intentionally transitional: modules own their
public API entry points and selected application/persistence pieces, while
shared `App.*` contract/persistence/domain projects remain available until
Phase 10 legacy dependency removal. Phase 10e removed the concrete `App.BLL`
implementation dependency from WebApp/modules. Phase 9 introduced module-owned
DbContext types, but the legacy `AppDbContext` still carries active
migrations/seeding/UOW runtime persistence until the remaining App.* dependency
center is removed.

Current boundaries:

- `WebApp` owns host composition, MVC areas, middleware pipeline setup, Swagger,
  and static client hosting.
- `Modules.Users` owns account API controllers, refresh-token persistence, and
  `UsersDbContext` for the identity/refresh-token persistence boundary.
- `Modules.Gyms` owns system/tenant gym API controllers, tenant resolution, and
  Gyms-owned platform/authorization services, and `GymsDbContext` for
  gym/settings/user-role persistence ownership.
- `Modules.Memberships` owns membership/member/payment tenant API controllers
  and member lookup projections. It also owns member, membership, membership
  package, and payment application services, mappers, repository contracts,
  and EF implementations, still backed by the active shared `AppDbContext`
  transitionally. `MembershipsDbContext` defines the
  member/membership/payment persistence boundary.
- `Modules.Training` owns staff, training category/session, and booking tenant
  API controllers, training workflow services, booking pricing, training EF
  repositories, and `ITrainingModuleApi` projections. `TrainingDbContext`
  defines the training/staff/booking persistence boundary.
- `Modules.Maintenance` owns equipment and maintenance-task tenant API
  controllers, maintenance workflow services, maintenance DTO mapping, and
  maintenance EF repositories. Staff assignment validation goes through
  `ITrainingModuleApi` instead of a direct Training module reference.
  `MaintenanceDbContext` defines the equipment/maintenance-task persistence
  boundary.
- `Shared.Contracts` owns cross-module DTO/projection contracts such as
  `IUsersModuleApi`, `IGymsModuleApi`, `IMembershipsModuleApi`, and
  `ITrainingModuleApi`.
- `App.BLL.Contracts` remains the transitional service-interface boundary for
  MVC and API consumers.
- `App.BLL` is now an empty compatibility project; WebApp/modules must not
  reference it.
- `App.DAL.Contracts` remains the transitional generic repository/UOW contract
  layer.
- `App.DAL.EF` still owns the shared `AppDbContext`, legacy EF configurations,
  migrations, and UOW runtime bridge until the remaining module services no
  longer depend on the App.* center.
- `App.Domain` still owns entities/enums until the physical persistence split.

Architecture tests currently enforce module existence, no direct
module-to-module project references, allowed transitional shared references,
WebApp references to all modules, no WebApp/module reference to concrete
`App.BLL`, module DbContext placement, and no foreign module DbContext
references. The full hard ban on remaining legacy `App.*` references is
deliberately skipped until Phase 10f.
