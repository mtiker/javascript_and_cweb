using System.ComponentModel.DataAnnotations;
using App.Domain.Common;

namespace App.Domain.Entities;

public class InvoiceLine : TenantBaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    [MaxLength(512)]
    public string Description { get; set; } = default!;

    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsCredit { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }
}
