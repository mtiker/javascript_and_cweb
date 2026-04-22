using App.Domain.Enums;

namespace App.DTO.v1.System.Support;

public class SupportTicketRequest
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Medium;
}
