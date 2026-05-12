using BuildingBlocks.Modules;

namespace Modules.Users;

/// <summary>
/// Marker type used by the architecture tests and module-discovery helpers.
/// Module-internal services are wired through
/// <see cref="UsersModuleServiceCollectionExtensions.AddUsersModule"/>.
/// </summary>
public sealed class UsersModule : IModule
{
    public string Name => "Users";
}
