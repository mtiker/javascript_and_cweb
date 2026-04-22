using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; }
    public Guid? GymId { get; set; }

    [MaxLength(128)]
    public string EntityName { get; set; } = default!;

    public Guid? EntityId { get; set; }

    [MaxLength(64)]
    public string Action { get; set; } = default!;

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ChangesJson { get; set; }
}
