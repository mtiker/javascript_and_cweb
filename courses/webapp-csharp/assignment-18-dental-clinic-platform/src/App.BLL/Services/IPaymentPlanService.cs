using App.BLL.Contracts.Finance;

namespace App.BLL.Services;

public interface IPaymentPlanService
{
    Task<IReadOnlyCollection<PaymentPlanResult>> ListAsync(Guid userId, Guid? invoiceId, CancellationToken cancellationToken);
    Task<PaymentPlanResult> GetAsync(Guid userId, Guid paymentPlanId, CancellationToken cancellationToken);
    Task<PaymentPlanResult> CreateAsync(Guid userId, CreatePaymentPlanCommand command, CancellationToken cancellationToken);
    Task<PaymentPlanResult> UpdateAsync(Guid userId, UpdatePaymentPlanCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid paymentPlanId, CancellationToken cancellationToken);
}
