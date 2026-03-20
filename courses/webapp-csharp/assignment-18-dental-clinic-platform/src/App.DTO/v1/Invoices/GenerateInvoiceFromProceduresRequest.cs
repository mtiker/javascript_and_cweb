using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Invoices;

public class GenerateInvoiceFromProceduresRequest
{
    [Required]
    public Guid PatientId { get; set; }

    public Guid? CostEstimateId { get; set; }

    [Required]
    [MaxLength(64)]
    public string InvoiceNumber { get; set; } = default!;

    [Required]
    public DateTime DueDateUtc { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> TreatmentIds { get; set; } = [];
}
