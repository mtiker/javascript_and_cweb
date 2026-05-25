namespace Shared.Contracts.ModuleApis;

/// <summary>
/// Public projection of a staff member owned by the Training module. Status is
/// the enum name as a string so Shared.Contracts stays App.Domain-free.
/// </summary>
public sealed record StaffSummary(
    Guid Id,
    Guid GymId,
    string StaffCode,
    string FullName,
    string Status);
