namespace App.DTO.v1.Finance;

public class PerformedProcedureResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid TreatmentTypeId { get; set; }
    public Guid? PlanItemId { get; set; }
    public Guid? AppointmentId { get; set; }
    public int? ToothNumber { get; set; }
    public DateTime PerformedAtUtc { get; set; }
    public decimal Price { get; set; }
    public string TreatmentTypeName { get; set; } = default!;
    public string? Notes { get; set; }
}
