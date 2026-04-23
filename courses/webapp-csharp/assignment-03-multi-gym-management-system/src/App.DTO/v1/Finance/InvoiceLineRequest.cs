namespace App.DTO.v1.Finance;

public class InvoiceLineRequest
{
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public bool IsCredit { get; set; }
    public string? Notes { get; set; }
}
