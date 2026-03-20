using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Payments;

public class CreatePaymentRequest
{
    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaidAtUtc { get; set; }

    [Required]
    [MaxLength(64)]
    public string Method { get; set; } = default!;

    [MaxLength(128)]
    public string? Reference { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }
}
