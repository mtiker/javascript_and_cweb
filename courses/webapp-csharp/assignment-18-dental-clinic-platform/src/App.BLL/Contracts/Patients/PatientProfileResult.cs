namespace App.BLL.Contracts.Patients;

public sealed record PatientProfileResult(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PersonalCode,
    string? Email,
    string? Phone,
    IReadOnlyCollection<PatientToothResult> Teeth);
