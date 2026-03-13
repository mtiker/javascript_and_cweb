namespace App.DTO.v1.Patients;

public class PatientResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public DateOnly? DateOfBirth { get; set; }
    public string? PersonalCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
