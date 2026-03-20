namespace App.DTO.v1.Invoices;

public class InvoiceLineResponse
{
    public Guid Id { get; set; }
    public Guid? TreatmentId { get; set; }
    public Guid? PlanItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal PatientAmount { get; set; }
}
