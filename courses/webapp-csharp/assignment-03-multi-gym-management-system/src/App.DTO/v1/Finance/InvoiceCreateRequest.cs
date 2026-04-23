namespace App.DTO.v1.Finance;

public class InvoiceCreateRequest
{
    public Guid MemberId { get; set; }
    public DateTime DueAtUtc { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string? Notes { get; set; }
    public IReadOnlyCollection<InvoiceLineRequest> Lines { get; set; } = [];
}
