using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.TreatmentPlans;

public class CreateTreatmentPlanRequest
{
    [Required]
    public Guid PatientId { get; set; }

    public Guid? DentistId { get; set; }

    [Required]
    [MinLength(1)]
    public ICollection<TreatmentPlanItemRequest> Items { get; set; } = new List<TreatmentPlanItemRequest>();
}
