using App.Domain.Common;

namespace App.Domain.Entities;

public class Xray : TenantBaseEntity
{
    public Guid PatientId { get; set; }
    public DateTime TakenAtUtc { get; set; }
    public DateTime? NextDueAtUtc { get; set; }
    public string StoragePath { get; set; } = default!;
    public string? Notes { get; set; }

    public Patient? Patient { get; set; }
}
