using MediatR;

namespace Shared.Contracts.Mediator;

/// <summary>
/// Marker for cross-module commands/queries. A module publishes the request
/// type from <c>Shared.Contracts</c>; the owning module supplies the handler.
/// Callers never depend on another module's internal request types.
/// </summary>
public interface IModuleRequest<TResponse> : IRequest<TResponse>
{
}
