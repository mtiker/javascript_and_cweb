using System.ComponentModel.DataAnnotations;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class Invoice : TenantBaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    [MaxLength(64)]
    public string InvoiceNumber { get; set; } = default!;

    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueAtUtc { get; set; }

    [MaxLength(8)]
    public string CurrencyCode { get; set; } = "EUR";

    public decimal SubtotalAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();
}
