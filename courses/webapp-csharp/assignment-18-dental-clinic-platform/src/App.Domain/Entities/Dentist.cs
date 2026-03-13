using App.Domain.Common;

namespace App.Domain.Entities;

public class Dentist : TenantBaseEntity
{
    public Guid? AppUserId { get; set; }
    public string DisplayName { get; set; } = default!;
    public string LicenseNumber { get; set; } = default!;
    public string? Specialty { get; set; }

    public App.Domain.Identity.AppUser? AppUser { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
