namespace App.DTO.v1.TreatmentPlans;

public class OpenPlanItemResponse
{
    public Guid PlanId { get; set; }
    public Guid PlanItemId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = default!;
    public string TreatmentTypeName { get; set; } = default!;
    public int Sequence { get; set; }
    public string Urgency { get; set; } = default!;
    public decimal EstimatedPrice { get; set; }
}
