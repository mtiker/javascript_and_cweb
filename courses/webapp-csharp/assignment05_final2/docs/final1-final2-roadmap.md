# Final1 Roadmap

The active architecture is Final1: root-level projects, service contracts,
business services, EF persistence, MVC/API WebApp, and root-level tests.

## Current Baseline

- Final1 root project layout is complete.
- The old module architecture has been removed from active code.
- API routes and DTO shapes remain stable.
- EF migrations remain in `App.DAL.EF/Migrations`.
- The React client remains under `client/`.

## Maintenance Priorities

1. Keep route and DTO docs aligned with `WebApp/ApiControllers` and `App.DTO/v1`.
2. Keep business rules documented in `domain-workflows.md`.
3. Keep deployment and validation commands current in `deployment.md` and
   `testing.md`.
4. Avoid adding presentation, EF, or client dependencies to `App.Domain`.
5. Keep WebApp controllers thin by delegating workflow decisions to BLL
   services.

## Future Final2 Note

The removed module implementation is not part of the current architecture.
Future modular-monolith work should be planned as a new deliberate phase from
the stable Final1 baseline.
