using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class SupportTicket : BaseEntity, ITenantEntity
{
    public Guid GymId { get; set; }
    public Gym? Gym { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    [MaxLength(128)]
    public string Title { get; set; } = default!;

    [MaxLength(2000)]
    public string Description { get; set; } = default!;

    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Medium;
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
}
