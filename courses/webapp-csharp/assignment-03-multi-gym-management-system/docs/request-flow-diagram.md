# Request Flow Diagram

## API / React Client Flow

```mermaid
sequenceDiagram
    autonumber
    participant Client as React client / API caller
    participant API as WebApp API controller
    participant GymMW as GymResolutionMiddleware
    participant Mediator as BuildingBlocks IMediator
    participant Module as Owning module handler
    participant BLL as Workflow service
    participant EF as AppDbContext / EF Core
    participant DB as PostgreSQL

    Client->>API: HTTP request + JWT + /api/v1/{gymCode}/...
    API->>GymMW: Resolve route gymCode
    GymMW->>DB: Query gym by code + active status
    GymMW-->>API: Continue or reject unknown/inactive gym

    API->>Mediator: Send module request/command
    Mediator->>Module: Dispatch to registered handler
    Module->>BLL: Execute existing workflow boundary
    BLL->>EF: Authorize active gym, role, self-only/IDOR rules
    EF->>DB: Query tenant-scoped data
    BLL->>EF: Load/update owning module entities
    EF->>DB: Persist changes and audit records

    DB-->>EF: Stored data
    EF-->>BLL: Entity/read model data
    BLL-->>Module: DTO or domain exception
    Module-->>Mediator: DTO or propagated exception
    Mediator-->>API: Result
    API-->>Client: 200/201/204 or ProblemDetails
```

## MVC Flow

```mermaid
sequenceDiagram
    autonumber
    participant Browser as Browser
    participant MVC as WebApp MVC area
    participant Service as MVC view-model/service boundary
    participant BLL as Workflow service
    participant EF as AppDbContext / EF Core
    participant DB as PostgreSQL

    Browser->>MVC: Cookie-authenticated /Admin or /mvc-client request
    MVC->>Service: Build strongly typed view model
    Service->>BLL: Read role/tenant-scoped workflow data
    BLL->>EF: Apply tenant, role, soft-delete, and i18n rules
    EF->>DB: Query data
    DB-->>EF: Rows
    EF-->>BLL: Entities/read data
    BLL-->>Service: DTOs
    Service-->>MVC: View model
    MVC-->>Browser: Razor HTML
```

## Boundary Notes

- `WebApp` is the only composition root and route owner.
- Migrated REST controllers use `IMediator`; modules do not reference each
  other directly.
- Auth, tenant, IDOR, soft-delete, and i18n rules remain enforced inside the
  workflow/data layers.
- Public API routes stay stable while controllers switch from direct service
  calls to mediator commands/queries.
- MVC Admin uses strongly typed view models and no `ViewBag`/`ViewData`.
