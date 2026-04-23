using System.ComponentModel.DataAnnotations;
using App.Domain.Common;

namespace App.Domain.Entities;

public class InvoicePayment : TenantBaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public decimal Amount { get; set; }
    public bool IsRefund { get; set; }
    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(128)]
    public string? Reference { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }
}
