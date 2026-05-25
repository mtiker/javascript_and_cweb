using Shared.Contracts.Dtos.v1.Payments;

namespace App.BLL.Contracts.Services;

public interface IPaymentService
{
    Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, PaymentFilter? filter = null, CancellationToken cancellationToken = default);
    Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse> RefundPaymentAsync(string gymCode, Guid paymentId, PaymentRefundRequest request, CancellationToken cancellationToken = default);
}
