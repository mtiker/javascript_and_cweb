namespace App.DTO.v1.Dentists;

public class DentistResponse
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = default!;
    public string LicenseNumber { get; set; } = default!;
    public string? Specialty { get; set; }
}
