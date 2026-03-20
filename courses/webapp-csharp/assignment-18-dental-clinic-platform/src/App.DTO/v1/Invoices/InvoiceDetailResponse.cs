using App.DTO.v1.PaymentPlans;
using App.DTO.v1.Payments;

namespace App.DTO.v1.Invoices;

public class InvoiceDetailResponse : InvoiceResponse
{
    public IReadOnlyCollection<InvoiceLineResponse> Lines { get; set; } = Array.Empty<InvoiceLineResponse>();
    public IReadOnlyCollection<PaymentResponse> Payments { get; set; } = Array.Empty<PaymentResponse>();
    public PaymentPlanResponse? PaymentPlan { get; set; }
}
