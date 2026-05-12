using App.DTO.v1;
using App.DTO.v1.Identity;
using BuildingBlocks.Mediator;
using Modules.Users.Contracts;

namespace Modules.Users.Application.Auth;

internal sealed class LoginCommandHandler(IUsersSessionService sessionService) : IRequestHandler<LoginCommand, JwtResponse>
{
    public async Task<JwtResponse> HandleAsync(LoginCommand request, CancellationToken cancellationToken)
    {
        return await sessionService.LoginAsync(request.Request, cancellationToken);
    }
}

internal sealed class RefreshSessionCommandHandler(IUsersSessionService sessionService) : IRequestHandler<RefreshSessionCommand, JwtResponse>
{
    public async Task<JwtResponse> HandleAsync(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        return await sessionService.RefreshAsync(request.Request, cancellationToken);
    }
}

internal sealed class LogoutCommandHandler(IUsersSessionService sessionService) : IRequestHandler<LogoutCommand, Message>
{
    public async Task<Message> HandleAsync(LogoutCommand request, CancellationToken cancellationToken)
    {
        return await sessionService.LogoutAsync(cancellationToken);
    }
}

internal sealed class SwitchGymCommandHandler(IUsersSessionService sessionService) : IRequestHandler<SwitchGymCommand, JwtResponse>
{
    public async Task<JwtResponse> HandleAsync(SwitchGymCommand request, CancellationToken cancellationToken)
    {
        return await sessionService.SwitchGymAsync(request.Request, cancellationToken);
    }
}

internal sealed class SwitchRoleCommandHandler(IUsersSessionService sessionService) : IRequestHandler<SwitchRoleCommand, JwtResponse>
{
    public async Task<JwtResponse> HandleAsync(SwitchRoleCommand request, CancellationToken cancellationToken)
    {
        return await sessionService.SwitchRoleAsync(request.Request, cancellationToken);
    }
}
