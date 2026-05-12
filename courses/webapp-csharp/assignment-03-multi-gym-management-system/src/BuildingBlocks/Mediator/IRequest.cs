namespace BuildingBlocks.Mediator;

/// <summary>
/// Marker for void-returning mediator requests dispatched across module
/// boundaries (or within a module, when the mediator pipeline is desired).
/// </summary>
public interface IRequest
{
}

/// <summary>
/// Marker for value-returning mediator requests.
/// </summary>
/// <typeparam name="TResponse">Response payload type.</typeparam>
public interface IRequest<TResponse>
{
}
