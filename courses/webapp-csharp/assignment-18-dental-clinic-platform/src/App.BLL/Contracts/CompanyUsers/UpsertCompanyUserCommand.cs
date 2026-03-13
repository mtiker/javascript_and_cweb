namespace App.BLL.Contracts.CompanyUsers;

public sealed record UpsertCompanyUserCommand(
    string Email,
    string RoleName,
    bool IsActive,
    string? TemporaryPassword);
