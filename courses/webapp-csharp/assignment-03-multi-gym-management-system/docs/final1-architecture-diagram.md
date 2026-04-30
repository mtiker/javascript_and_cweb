# Final1 Architecture Diagram

**Date:** 2026-04-30

## Clean/Onion Dependency Direction

```mermaid
flowchart TB
    WebApp["WebApp\nMVC, REST API, Swagger, Middleware, DI"]
    React["client/\nReact + TypeScript"]
    BLL["App.BLL\nServices, business rules, contracts, mappers"]
    DTO["App.DTO\nPublic v1 API contracts"]
    Domain["App.Domain\nEntities, enums, shared domain abstractions"]
    DALEF["App.DAL.EF\nEF Core DbContext, repositories, UOW implementation"]
    DB[("PostgreSQL / EF test stores")]

    React -->|"HTTP /api/v1, JWT, Accept-Language"| WebApp
    WebApp -->|"uses services"| BLL
    WebApp -->|"reads/writes public contracts"| DTO
    BLL -->|"uses entities"| Domain
    BLL -->|"returns/accepts DTOs through mappers"| DTO
    BLL -->|"declares persistence contracts"| BLLContracts["App.BLL.Contracts.Persistence\nIRepository, IAppUnitOfWork, slice repositories"]
    DALEF -->|"implements contracts"| BLLContracts
    DALEF -->|"persists entities"| Domain
    DALEF --> DB
    WebApp -->|"composition root registers"| DALEF
```

Allowed direction:
- `WebApp -> App.BLL`
- `WebApp -> App.DTO`
- `App.BLL -> App.Domain`
- `App.BLL -> App.DTO`
- `App.DAL.EF -> App.BLL.Contracts`
- `App.DAL.EF -> App.Domain`
- `WebApp -> App.DAL.EF` only at the composition root and EF/Identity infrastructure setup

Forbidden direction:
- `App.Domain` must not reference `App.BLL`, `App.DAL.EF`, `App.DTO`, or `WebApp`.
- `App.DTO` must not reference `App.BLL`, `App.DAL.EF`, or `WebApp`.
- `App.BLL` must not reference `App.DAL.EF` or `WebApp`.
- `App.DAL.EF` must not reference `WebApp`.

These rules are enforced by `tests/WebApp.Tests/Architecture/ArchitectureTests.cs`.

## Final1 Request Flow

```mermaid
sequenceDiagram
    participant Client as React/MVC Client
    participant Web as WebApp Controller
    participant Auth as Auth/Tenant Middleware
    participant Bll as BLL Service
    participant Uow as IAppUnitOfWork
    participant Repo as EF Repository
    participant Db as AppDbContext

    Client->>Web: HTTP request
    Web->>Auth: authenticate JWT/cookie and resolve tenant context
    Auth-->>Web: user, active gym, active role
    Web->>Bll: call use-case service with DTO/request data
    Bll->>Uow: use repository contract
    Uow->>Repo: route to EF implementation
    Repo->>Db: query/mutate tenant-scoped entities
    Db-->>Repo: entities
    Repo-->>Uow: domain data
    Uow-->>Bll: result
    Bll-->>Web: DTO/result or domain exception
    Web-->>Client: DTO or ProblemDetails
```

## Final1 Boundary Evidence

| Slice | Controller/service boundary | Persistence boundary | Mapper boundary |
|-------|-----------------------------|----------------------|-----------------|
| Auth sessions | `AccountController -> IAccountAuthService` | `IAppUnitOfWork.RefreshTokens -> IRefreshTokenRepository -> EfRefreshTokenRepository` | `IAuthResponseMapper -> AuthResponseMapper` |
| Members | `MembersController -> IMemberWorkflowService` | `IAppUnitOfWork.Members -> IMemberRepository -> EfMemberRepository` | `IMemberMapper -> MemberMapper` |
| Training/bookings | Training controllers -> `ITrainingWorkflowService` | training/category/session/booking/work-shift repositories | `ITrainingMapper -> TrainingMapper` |
| Membership/finance | package, membership, payment, finance services | package/membership/payment/finance repositories | `IMembershipFinanceMapper -> MembershipFinanceMapper` |
| Maintenance | maintenance/facility controllers -> `IMaintenanceWorkflowService` | `IAppUnitOfWork.Maintenance -> IMaintenanceRepository -> EfMaintenanceRepository` | `IMaintenanceMapper -> MaintenanceMapper` |

## Tenant Isolation Path

```mermaid
flowchart LR
    JWT["JWT claims\nactive gym + role"] --> Route["/api/v1/{gymCode}/..."]
    Route --> TenantCheck["Tenant access check"]
    TenantCheck --> RoleCheck["Role/resource authorization"]
    RoleCheck --> Query["Repository query scoped by GymId"]
    Query --> Result["DTO response"]
    TenantCheck --> Deny["403/404 ProblemDetails"]
    RoleCheck --> Deny
```

Defense statement:
- The route gym code is not trusted by itself.
- The active gym in the authenticated session must match the route gym code unless a system-admin flow explicitly switches context.
- Resource IDs are always looked up inside the active gym scope for Final1-critical slices.
