namespace BuildingBlocks.Mediator;

/// <summary>
/// In-process dispatcher used to call across module boundaries without
/// referencing the target module's project. Handlers are resolved from
/// <see cref="IServiceProvider"/> per request.
/// </summary>
public interface IMediator
{
    Task SendAsync(IRequest request, CancellationToken cancellationToken = default);

    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
