using App.Domain.Common;

namespace App.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? CompanyId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EntityName { get; set; } = default!;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = default!;
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public string ChangesJson { get; set; } = "{}";
}
