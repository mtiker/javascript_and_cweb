namespace App.DTO.v1.Invoices;

public class InvoiceResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? CostEstimateId { get; set; }
    public string InvoiceNumber { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public DateTime DueDateUtc { get; set; }
    public string Status { get; set; } = default!;
}
