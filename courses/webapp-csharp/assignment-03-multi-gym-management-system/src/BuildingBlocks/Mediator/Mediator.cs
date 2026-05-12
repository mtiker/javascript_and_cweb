using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Mediator;

/// <summary>
/// Default <see cref="IMediator"/>. Closes the open generic handler interfaces
/// over the concrete request type and resolves a single handler per request
/// from the container. Registration is up to module DI extensions
/// (see <see cref="MediatorRegistration"/>).
/// </summary>
public sealed class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> VoidInvokerCache = new();
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), HandlerInvoker> ResultInvokerCache = new();

    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invoker = VoidInvokerCache.GetOrAdd(request.GetType(), CreateVoidInvoker);
        return invoker(_serviceProvider, request, cancellationToken);
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invoker = ResultInvokerCache.GetOrAdd(
            (request.GetType(), typeof(TResponse)),
            key => CreateResultInvoker(key.RequestType, key.ResponseType));
        return (Task<TResponse>)invoker(_serviceProvider, request, cancellationToken);
    }

    private static HandlerInvoker CreateVoidInvoker(Type requestType)
    {
        var handlerInterface = typeof(IRequestHandler<>).MakeGenericType(requestType);
        var method = handlerInterface.GetMethod(
            nameof(IRequestHandler<IRequest>.HandleAsync),
            BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"HandleAsync not found on {handlerInterface}.");

        return (provider, request, cancellationToken) =>
        {
            var handler = provider.GetService(handlerInterface)
                ?? throw new InvalidOperationException($"No handler registered for {requestType.FullName}.");
            return InvokeHandler(method, handler, request, cancellationToken);
        };
    }

    private static HandlerInvoker CreateResultInvoker(Type requestType, Type responseType)
    {
        var handlerInterface = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var method = handlerInterface.GetMethod(
            "HandleAsync",
            BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"HandleAsync not found on {handlerInterface}.");

        return (provider, request, cancellationToken) =>
        {
            var handler = provider.GetService(handlerInterface)
                ?? throw new InvalidOperationException($"No handler registered for {requestType.FullName}.");
            return InvokeHandler(method, handler, request, cancellationToken);
        };
    }

    private static Task InvokeHandler(MethodInfo method, object handler, object request, CancellationToken cancellationToken)
    {
        try
        {
            return (Task)method.Invoke(handler, new object[] { request, cancellationToken })!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private delegate Task HandlerInvoker(IServiceProvider provider, object request, CancellationToken cancellationToken);
}
