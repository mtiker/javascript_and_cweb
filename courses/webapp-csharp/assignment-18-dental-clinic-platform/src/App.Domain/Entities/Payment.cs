using App.Domain.Common;

namespace App.Domain.Entities;

public class Payment : TenantBaseEntity
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public string Method { get; set; } = default!;
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Invoice? Invoice { get; set; }
}
