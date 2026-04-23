using App.Domain.Enums;

namespace App.DTO.v1.Finance;

public class InvoiceResponse
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = default!;
    public string InvoiceNumber { get; set; } = default!;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public decimal SubtotalAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public bool IsOverdue { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyCollection<InvoiceLineResponse> Lines { get; set; } = [];
    public IReadOnlyCollection<InvoicePaymentResponse> Payments { get; set; } = [];
}
