# Mediator Design (Final-2)

**Companion to:** `docs/final2-module-plan.md`,
`docs/module-boundary-audit.md`, `docs/module-data-ownership.md`.

This document explains the in-process mediator that lives in
`BuildingBlocks/Mediator/`. The mediator is the **only** runtime path used
when one Final-2 module needs something from another module.

---

## 1. Why a custom mediator (and not MediatR)

- The course wants a defendable modular monolith. We need to **explain**
  the seam, not delegate it to a framework. A 60-line mediator is easier to
  defend than a third-party dependency.
- MediatR adds a runtime + nuget dep + version pinning that the assignment
  does not require. Final-2 only needs `Send` semantics; pipeline behaviors
  are out of scope.
- We can lift to MediatR later by replacing the implementation file —
  request types and handlers are framework-shape compatible.

---

## 2. Contract

Lives in `BuildingBlocks/Mediator/`:

```csharp
namespace BuildingBlocks.Mediator;

// marker for void-returning requests
public interface IRequest { }

// marker for value-returning requests
public interface IRequest<TResponse> { }

public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    Task HandleAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IMediator
{
    Task SendAsync(IRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
```

`Mediator` resolves the matching handler from `IServiceProvider` per request.
Handlers are registered scoped, exactly once each. Resolution is by closed
generic type — the mediator never reflects over assemblies at request time.

---

## 3. Naming conventions

| Element | Convention | Example |
|---|---|---|
| Query type | `Get…Query`, `Find…Query`, `List…Query`, `Ensure…Query` | `GetUserContextQuery` |
| Command type | `…Command` | `WriteAuditLogCommand` |
| Response DTO | `…Result` or `…Dto` (DTO if shared, Result if return-only) | `UserContextDto` |
| Handler | `…Handler` (one per type) | `GetUserContextQueryHandler` |
| File layout (handler is internal to the module) | `Modules.<Name>/Application/<Slice>/<Type>.cs` and `…Handler.cs` | `Modules.Users/Application/Context/GetUserContextQuery.cs` |
| Public request type (file in module's `Public/` folder, type lives in `BuildingBlocks.Contracts`) | the type itself lives under `BuildingBlocks.Contracts.<Module>.<Slice>` | `BuildingBlocks.Contracts.Users.GetUserContextQuery` |

> **Where does a request type live?**
>
> - **Module-public** (other modules can send it): in
>   `BuildingBlocks.Contracts.<Module>.<Slice>`. This keeps the cross-module
>   surface visible without giving callers a project reference to the
>   module.
> - **Module-internal** (only the owning module sends it): in
>   `Modules.<Name>.Application.<Slice>` with `internal` accessibility. Used
>   when a module wants the mediator pipeline (e.g. logging, transactions)
>   for its own use cases later, but doesn't want to publish the contract.
>
> Phase 16 lands the abstractions and folder layout; cross-module request
> types are added as the corresponding slice migrates.

---

## 4. Registration

### 4.1 Mediator infrastructure (BuildingBlocks)

```csharp
public static class BuildingBlocksServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        return services;
    }
}
```

### 4.2 Per-module handler registration

Each module's DI extension registers handlers from its own assembly. We use
a small reflection-based scan executed once at startup:

```csharp
public static IServiceCollection AddModuleMediatorHandlersFromAssembly(
    this IServiceCollection services,
    Assembly assembly)
{
    var openHandlerInterfaces = new[]
    {
        typeof(IRequestHandler<>),
        typeof(IRequestHandler<,>)
    };

    foreach (var type in assembly.GetTypes())
    {
        if (!type.IsClass || type.IsAbstract) continue;

        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;
            var def = iface.GetGenericTypeDefinition();
            if (Array.IndexOf(openHandlerInterfaces, def) < 0) continue;
            services.AddScoped(iface, type);
        }
    }

    return services;
}
```

Module example:

```csharp
public static class UsersModuleServiceCollectionExtensions
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddModuleMediatorHandlersFromAssembly(typeof(UsersModuleServiceCollectionExtensions).Assembly);
        // module-internal services here as slices migrate
        return services;
    }
}
```

`Program.cs` orchestration:

```csharp
builder.Services.AddBuildingBlocks();
builder.Services.AddUsersModule();
builder.Services.AddGymManagementModule();
builder.Services.AddTrainingModule();
builder.Services.AddMembershipFinanceModule();
```

Order is alphabetical; modules must not depend on each other's registration
order. If a startup-time concern surfaces, we add an explicit
`IModuleStartup` host service rather than relying on registration order.

---

## 5. Cross-module call examples (planned, not yet implemented)

### 5.0 Users auth/session messages (implemented in Phase 17)

`AccountController` now dispatches the account-session routes through
`IMediator` using public Users module messages:

- `LoginCommand`
- `RefreshSessionCommand`
- `LogoutCommand`
- `SwitchGymCommand`
- `SwitchRoleCommand`

The messages live in `Modules.Users.Contracts`; handlers live internally under
`Modules.Users.Application.Auth`. See `users-mediator-messages.md` for the
full message map.

### 5.1 Training reads tenant context

```csharp
// in BuildingBlocks.Contracts.Users.Context
public sealed record GetUserContextQuery(Guid UserId) : IRequest<UserContextDto>;
public sealed record UserContextDto(Guid UserId, Guid? ActiveGymId, IReadOnlyList<string> Roles);

// inside Modules.Users.Application.Context (internal handler)
internal sealed class GetUserContextQueryHandler : IRequestHandler<GetUserContextQuery, UserContextDto>
{
    public Task<UserContextDto> HandleAsync(GetUserContextQuery request, CancellationToken cancellationToken) { /* ... */ }
}

// caller in Modules.Training
var context = await mediator.SendAsync(new GetUserContextQuery(userId), cancellationToken);
```

### 5.2 MembershipFinance writes audit

```csharp
// in BuildingBlocks.Contracts.GymManagement.Audit
public sealed record WriteAuditLogCommand(Guid GymId, string Action, string Detail) : IRequest;

// caller
await mediator.SendAsync(new WriteAuditLogCommand(gymId, "PaymentRefunded", paymentId.ToString()));
```

---

## 6. Error semantics

- A handler throws → the exception propagates. The mediator does not swallow
  or wrap exceptions.
- The HTTP layer's existing `ProblemDetailsMiddleware` continues to handle
  conversion to `application/problem+json` for API requests.
- Validation that should reject a request lives **inside** the handler (or
  via FluentValidation later); the mediator itself is purely transport.

---

## 7. Cancellation, scope, lifetime

- All handlers are **scoped** to the ASP.NET request scope.
- `IMediator` is **scoped**; resolving it within a controller or hosted
  service uses the request scope.
- Handlers must accept and forward `CancellationToken`. Architecture
  guideline: every handler signature includes `CancellationToken`. Future
  test can enforce this once we have handlers.

---

## 8. What's intentionally NOT in scope for Final-2

- **Pipeline behaviors** (logging, validation, transactions). If a phase
  needs a transaction wrapping multiple module commands, we add it via a
  small decorator, not a generic pipeline.
- **In-process pub/sub events** (`INotification`/`INotificationHandler`).
  Domain events stay deferred until a slice has a clear use case; the
  mediator's `Send` is the only path used in Final-2.
- **Cross-process bus.** Final-3 (microservice extraction) is the right
  time for that, not Final-2.

---

## 9. Phase 16 deliverables (this phase)

| Item | File |
|---|---|
| Marker interfaces | `src/BuildingBlocks/Mediator/IRequest.cs` |
| Handler interfaces | `src/BuildingBlocks/Mediator/IRequestHandler.cs` |
| Mediator interface | `src/BuildingBlocks/Mediator/IMediator.cs` |
| Mediator implementation | `src/BuildingBlocks/Mediator/Mediator.cs` |
| Handler scan helper | `src/BuildingBlocks/Mediator/MediatorRegistration.cs` |
| BuildingBlocks DI extension | `src/BuildingBlocks/BuildingBlocksServiceCollectionExtensions.cs` |
| Module marker | `src/BuildingBlocks/Modules/IModule.cs` |
| Module DI extensions | one per module |
| Wired in startup | `src/WebApp/Setup/ModuleExtensions.cs` |

---

## 10. Defense one-liner

> *"Modules talk via an in-process mediator. Request types and DTOs are the
> only public surface; handlers are internal. A startup-time scan registers
> the handlers for each module. The whole abstraction is six small files in
> BuildingBlocks — no third-party runtime — so the seam is easy to defend
> and easy to evolve into a real bus when we extract a microservice."*
