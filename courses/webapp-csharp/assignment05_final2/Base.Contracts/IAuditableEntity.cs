namespace Base.Contracts;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    DateTime? ModifiedAtUtc { get; set; }
    Guid? CreatedByUserId { get; set; }
    Guid? ModifiedByUserId { get; set; }
}
