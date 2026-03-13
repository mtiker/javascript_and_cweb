namespace App.DTO.v1.TreatmentPlans;

public class PlanItemDecisionResponse
{
    public Guid PlanId { get; set; }
    public Guid PlanItemId { get; set; }
    public string PlanStatus { get; set; } = default!;
    public string ItemDecision { get; set; } = default!;
}
