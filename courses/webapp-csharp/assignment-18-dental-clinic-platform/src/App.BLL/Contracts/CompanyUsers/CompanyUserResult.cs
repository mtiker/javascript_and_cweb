namespace App.BLL.Contracts.CompanyUsers;

public sealed record CompanyUserResult(
    Guid AppUserId,
    string Email,
    string RoleName,
    bool IsActive,
    DateTime AssignedAtUtc);
