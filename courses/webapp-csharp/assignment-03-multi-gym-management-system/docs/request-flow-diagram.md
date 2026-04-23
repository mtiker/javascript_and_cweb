# Request Flow Diagram

```mermaid
sequenceDiagram
    autonumber
    participant Client as React/MVC Client
    participant API as WebApp API
    participant GymMW as GymResolutionMiddleware
    participant AuthZ as BLL AuthorizationService
    participant BLL as Workflow Service
    participant EF as AppDbContext/EF Core
    participant DB as PostgreSQL

    Client->>API: HTTP request + JWT + /api/v1/{gymCode}/...
    API->>GymMW: Resolve route gymCode
    GymMW->>DB: Query gym by code + active status
    GymMW-->>API: Reject unknown/inactive gym (404/403) or continue

    API->>AuthZ: Validate active gym claim + role + self/assignment rules
    AuthZ->>EF: Tenant and subject checks
    EF->>DB: Query tenant-scoped data
    AuthZ-->>API: Allow or throw Forbidden/NotFound/Validation

    API->>BLL: Execute workflow action
    BLL->>EF: Load/update tenant entities (GymId-bound)
    EF->>DB: Persist changes
    BLL->>EF: Write audit log when required
    EF->>DB: Commit (incl. audit)

    EF-->>BLL: Return DTO-projected data
    BLL-->>API: Success DTO or domain exception
    API-->>Client: 200/201/204 or ProblemDetails (400/401/403/404/409)
```

## Notes
- Route middleware gives early tenant validation evidence.
- BLL authorization remains the final enforcement layer.
- EF soft-delete filters hide logically deleted tenant rows.
- Audit writes are persisted for security-sensitive actions.
