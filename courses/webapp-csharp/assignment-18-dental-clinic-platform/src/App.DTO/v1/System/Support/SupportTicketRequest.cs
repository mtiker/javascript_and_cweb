using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.System.Support;

public class SupportTicketRequest
{
    [Required]
    [MaxLength(64)]
    public string CompanySlug { get; set; } = default!;

    [Required]
    [MaxLength(160)]
    public string Subject { get; set; } = default!;

    [Required]
    [MaxLength(2000)]
    public string Details { get; set; } = default!;
}
