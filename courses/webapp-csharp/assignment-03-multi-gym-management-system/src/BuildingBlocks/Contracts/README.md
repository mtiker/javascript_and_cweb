# BuildingBlocks.Contracts

Cross-module mediator request/response types and shared contract DTOs live
here. Keep this folder intentionally small; add types only when a migrated
module boundary needs a shared contract.

Layout convention:
- `BuildingBlocks.Contracts.<Module>.<Slice>.<TypeName>`
- example: `BuildingBlocks.Contracts.Users.Context.GetUserContextQuery`

See `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/module-boundaries.md`
for module boundary, mediator, and registration rules.
