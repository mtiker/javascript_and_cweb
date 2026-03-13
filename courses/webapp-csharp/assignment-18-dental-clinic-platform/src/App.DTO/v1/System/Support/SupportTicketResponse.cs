namespace App.DTO.v1.System.Support;

public class SupportTicketResponse
{
    public Guid TicketId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Details { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}
