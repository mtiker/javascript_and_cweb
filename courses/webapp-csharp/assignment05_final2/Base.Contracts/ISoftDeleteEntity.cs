namespace Base.Contracts;

public interface ISoftDeleteEntity
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
