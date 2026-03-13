using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Invoices;

public class UpdateInvoiceRequest
{
    [Required]
    public Guid PatientId { get; set; }

    public Guid? CostEstimateId { get; set; }

    [Required]
    [MaxLength(64)]
    public string InvoiceNumber { get; set; } = default!;

    [Range(0, 999999999)]
    public decimal TotalAmount { get; set; }

    [Range(0, 999999999)]
    public decimal BalanceAmount { get; set; }

    [Required]
    public DateTime DueDateUtc { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = default!;
}
