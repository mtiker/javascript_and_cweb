using Base.Contracts;

namespace Base.Domain;

public abstract class BaseEntity : IBaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
