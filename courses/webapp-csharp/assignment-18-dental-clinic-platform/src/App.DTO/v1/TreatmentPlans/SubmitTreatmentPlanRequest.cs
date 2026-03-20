using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.TreatmentPlans;

public class SubmitTreatmentPlanRequest
{
    [MaxLength(512)]
    public string? Notes { get; set; }
}
