namespace App.BLL.Contracts.Patients;

public sealed record CreatePatientCommand(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PersonalCode,
    string? Email,
    string? Phone);
