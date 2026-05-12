# BuildingBlocks.Contracts

Cross-module mediator request/response types and shared contract DTOs live
here. The folder is intentionally empty in Phase 16 — types are added when
the corresponding slice migrates in Phases 17–20.

Layout convention:
- `BuildingBlocks.Contracts.<Module>.<Slice>.<TypeName>`
- e.g. `BuildingBlocks.Contracts.Users.Context.GetUserContextQuery`.

See `docs/mediator-design.md` for naming and registration rules.
