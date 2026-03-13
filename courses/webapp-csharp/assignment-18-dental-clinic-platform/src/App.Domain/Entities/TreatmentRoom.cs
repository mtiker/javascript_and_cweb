using App.Domain.Common;

namespace App.Domain.Entities;

public class TreatmentRoom : TenantBaseEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public bool IsActiveRoom { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
