using App.BLL.Contracts.Finance;

namespace App.BLL.Services;

public interface IInvoiceService
{
    Task<IReadOnlyCollection<InvoiceSummaryResult>> ListAsync(Guid userId, Guid? patientId, CancellationToken cancellationToken);
    Task<InvoiceDetailResult> GetAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken);
    Task<InvoiceDetailResult> CreateAsync(Guid userId, CreateInvoiceCommand command, CancellationToken cancellationToken);
    Task<InvoiceDetailResult> GenerateFromProceduresAsync(Guid userId, GenerateInvoiceFromProceduresCommand command, CancellationToken cancellationToken);
    Task<PaymentResult> AddPaymentAsync(Guid userId, Guid invoiceId, CreatePaymentCommand command, CancellationToken cancellationToken);
    Task<InvoiceDetailResult> UpdateAsync(Guid userId, UpdateInvoiceCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken);
}
