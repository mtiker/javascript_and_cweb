using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Invoices;

public class InvoiceLineRequest
{
    public Guid? TreatmentId { get; set; }
    public Guid? PlanItemId { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }

    [Range(0.01, 1000)]
    public decimal Quantity { get; set; } = 1m;

    [Range(0, 999999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, 999999999)]
    public decimal CoverageAmount { get; set; }
}
