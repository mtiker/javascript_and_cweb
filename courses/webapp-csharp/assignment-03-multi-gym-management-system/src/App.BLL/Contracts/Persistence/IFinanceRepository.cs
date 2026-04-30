using App.Domain.Entities;

namespace App.BLL.Contracts.Persistence;

public interface IFinanceRepository
{
    Task<IReadOnlyList<Invoice>> ListInvoicesAsync(Guid gymId, Guid? memberId, CancellationToken cancellationToken = default);
    Task<Invoice?> FindInvoiceAsync(Guid gymId, Guid invoiceId, CancellationToken cancellationToken = default);
    Task<int> CountInvoicesIssuedBetweenAsync(Guid gymId, DateTime fromUtc, DateTime untilUtc, CancellationToken cancellationToken = default);
    Task AddInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task AddInvoicePaymentAsync(InvoicePayment invoicePayment, CancellationToken cancellationToken = default);
}
