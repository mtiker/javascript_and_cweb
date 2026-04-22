using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.BLL.Services;

public sealed record UserExecutionContext(
    Guid? UserId,
    Guid? PersonId,
    Guid? ActiveGymId,
    string? ActiveGymCode,
    string? ActiveRole,
    IReadOnlyCollection<string> AllRoles,
    IReadOnlyCollection<string> SystemRoles)
{
    public bool IsAuthenticated => UserId.HasValue;
    public bool HasRole(string roleName) => AllRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    public bool HasAnyRole(params IEnumerable<string> roleNames) => roleNames.Any(HasRole);
}
