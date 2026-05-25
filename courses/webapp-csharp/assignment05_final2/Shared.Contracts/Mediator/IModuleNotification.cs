using MediatR;

namespace Shared.Contracts.Mediator;

/// <summary>
/// Marker for cross-module domain/application events. Modules publish and
/// subscribe to these via <see cref="IMediator"/>. Concrete events live in
/// <c>Shared.Contracts</c> so no module needs a direct reference to another.
/// </summary>
public interface IModuleNotification : INotification
{
}
