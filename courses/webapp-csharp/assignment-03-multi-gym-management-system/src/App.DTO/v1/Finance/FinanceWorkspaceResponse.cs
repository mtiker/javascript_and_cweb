namespace App.DTO.v1.Finance;

public class FinanceWorkspaceResponse
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = default!;
    public string MemberCode { get; set; } = default!;
    public decimal OutstandingBalance { get; set; }
    public decimal TotalRefundCredits { get; set; }
    public int OverdueInvoiceCount { get; set; }
    public IReadOnlyCollection<InvoiceResponse> Invoices { get; set; } = [];
    public IReadOnlyCollection<InvoicePaymentResponse> PaymentHistory { get; set; } = [];
}
