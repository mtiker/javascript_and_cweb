namespace App.BLL.Contracts.Patients;

public sealed record UpdatePatientCommand(
    Guid PatientId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PersonalCode,
    string? Email,
    string? Phone);
