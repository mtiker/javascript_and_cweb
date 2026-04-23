using App.DTO.v1.Payments;

namespace App.BLL.Services;

public interface IPaymentService
{
    Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default);
}
