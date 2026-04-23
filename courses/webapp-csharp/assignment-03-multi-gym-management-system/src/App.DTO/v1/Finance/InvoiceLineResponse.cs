namespace App.DTO.v1.Finance;

public class InvoiceLineResponse
{
    public Guid Id { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsCredit { get; set; }
    public string? Notes { get; set; }
}
