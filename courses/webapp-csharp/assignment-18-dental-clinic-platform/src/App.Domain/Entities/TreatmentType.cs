using App.Domain.Common;

namespace App.Domain.Entities;

public class TreatmentType : TenantBaseEntity
{
    public string Name { get; set; } = default!;
    public int DefaultDurationMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public string? Description { get; set; }

    public ICollection<PlanItem> PlanItems { get; set; } = new List<PlanItem>();
}
