namespace App.Domain.Common;

public interface ISoftDeleteEntity
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
