namespace App.DTO.v1.ToothRecords;

public class ToothRecordResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public int ToothNumber { get; set; }
    public string Condition { get; set; } = default!;
    public string? Notes { get; set; }
}
