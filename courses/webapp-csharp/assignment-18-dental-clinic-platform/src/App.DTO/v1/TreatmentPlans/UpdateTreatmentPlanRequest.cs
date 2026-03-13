using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.TreatmentPlans;

public class UpdateTreatmentPlanRequest
{
    public Guid? PatientId { get; set; }

    public Guid? DentistId { get; set; }

    [MaxLength(32)]
    public string? Status { get; set; }

    public ICollection<TreatmentPlanItemRequest>? Items { get; set; }
}
