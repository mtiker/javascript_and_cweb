namespace App.DTO.v1.TreatmentPlans;

public class TreatmentPlanResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DentistId { get; set; }
    public string Status { get; set; } = default!;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public bool IsLocked { get; set; }
    public ICollection<TreatmentPlanItemResponse> Items { get; set; } = new List<TreatmentPlanItemResponse>();
}
