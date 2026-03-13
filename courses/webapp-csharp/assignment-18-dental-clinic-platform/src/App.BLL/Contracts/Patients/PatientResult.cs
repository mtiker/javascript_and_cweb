namespace App.BLL.Contracts.Patients;

public sealed record PatientResult(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PersonalCode,
    string? Email,
    string? Phone);
