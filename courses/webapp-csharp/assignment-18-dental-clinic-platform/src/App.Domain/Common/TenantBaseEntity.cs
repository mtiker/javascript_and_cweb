namespace App.Domain.Common;

public abstract class TenantBaseEntity : BaseEntity, ITenantEntity, IAuditableEntity, ISoftDeleteEntity
{
    public Guid CompanyId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
