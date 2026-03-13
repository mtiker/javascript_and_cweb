using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.System.Billing;

public class UpdateInvoiceStatusRequest
{
    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = default!;
}
