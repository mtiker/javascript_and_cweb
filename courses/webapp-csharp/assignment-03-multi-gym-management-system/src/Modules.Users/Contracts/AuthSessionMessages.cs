using App.DTO.v1;
using App.DTO.v1.Identity;
using BuildingBlocks.Mediator;

namespace Modules.Users.Contracts;

public sealed record LoginCommand(LoginRequest Request) : IRequest<JwtResponse>;

public sealed record RefreshSessionCommand(RefreshTokenRequest Request) : IRequest<JwtResponse>;

public sealed record LogoutCommand : IRequest<Message>;

public sealed record SwitchGymCommand(SwitchGymRequest Request) : IRequest<JwtResponse>;

public sealed record SwitchRoleCommand(SwitchRoleRequest Request) : IRequest<JwtResponse>;
