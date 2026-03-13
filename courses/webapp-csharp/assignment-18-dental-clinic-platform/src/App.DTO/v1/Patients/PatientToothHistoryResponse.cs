namespace App.DTO.v1.Patients;

public class PatientToothHistoryResponse
{
    public Guid Id { get; set; }
    public Guid? AppointmentId { get; set; }
    public string TreatmentTypeName { get; set; } = default!;
    public DateTime PerformedAtUtc { get; set; }
    public decimal Price { get; set; }
    public string? Notes { get; set; }
}
