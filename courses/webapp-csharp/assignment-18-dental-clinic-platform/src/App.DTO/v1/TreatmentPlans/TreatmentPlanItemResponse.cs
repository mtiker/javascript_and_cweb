namespace App.DTO.v1.TreatmentPlans;

public class TreatmentPlanItemResponse
{
    public Guid Id { get; set; }
    public Guid TreatmentTypeId { get; set; }
    public string TreatmentTypeName { get; set; } = default!;
    public int Sequence { get; set; }
    public string Urgency { get; set; } = default!;
    public decimal EstimatedPrice { get; set; }
    public string Decision { get; set; } = default!;
    public DateTime? DecisionAtUtc { get; set; }
    public string? DecisionNotes { get; set; }
}
