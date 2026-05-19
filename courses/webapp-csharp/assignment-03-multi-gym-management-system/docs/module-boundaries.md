# Module Architecture Status

The earlier module architecture has been removed from the active codebase. This
document is retained only to avoid broken documentation links.

Current Final1 boundaries are:

- `WebApp` owns HTTP/MVC presentation and application composition.
- `App.BLL.Contracts` owns application service interfaces.
- `App.BLL` owns business workflow services and mappers.
- `App.DAL.Contracts` owns repository and unit-of-work contracts.
- `App.DAL.EF` owns EF Core persistence implementations and migrations.
- `App.Domain` owns domain entities, enums, and identity domain classes.
- `Base.Contracts`, `Base.Domain`, and `Base.Helpers` own reusable primitives.

Controllers should call BLL contract services directly. Module-specific mediator
dispatch and module DI registration are no longer active Final1 architecture.
