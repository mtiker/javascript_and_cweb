namespace App.DTO.v1.System.Billing;

public class InvoiceSummaryResponse
{
    public Guid InvoiceId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = default!;
    public string CompanySlug { get; set; } = default!;
    public string InvoiceNumber { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = default!;
    public DateTime DueDateUtc { get; set; }
}
