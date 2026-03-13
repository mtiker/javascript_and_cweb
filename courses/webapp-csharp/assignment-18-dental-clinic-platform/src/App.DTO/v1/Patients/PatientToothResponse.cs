namespace App.DTO.v1.Patients;

public class PatientToothResponse
{
    public Guid Id { get; set; }
    public int ToothNumber { get; set; }
    public string Condition { get; set; } = default!;
    public string? Notes { get; set; }
    public DateTime StatusUpdatedAtUtc { get; set; }
    public DateTime? LastTreatmentAtUtc { get; set; }
    public string? LastTreatmentTypeName { get; set; }
    public string? LastTreatmentNotes { get; set; }
    public IReadOnlyCollection<PatientToothHistoryResponse> History { get; set; } = Array.Empty<PatientToothHistoryResponse>();
}
