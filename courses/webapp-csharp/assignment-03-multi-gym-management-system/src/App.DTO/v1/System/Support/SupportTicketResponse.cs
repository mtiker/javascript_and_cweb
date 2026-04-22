using App.Domain.Enums;

namespace App.DTO.v1.System.Support;

public class SupportTicketResponse
{
    public Guid TicketId { get; set; }
    public Guid GymId { get; set; }
    public string GymName { get; set; } = default!;
    public string Title { get; set; } = default!;
    public SupportTicketStatus Status { get; set; }
    public SupportTicketPriority Priority { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
