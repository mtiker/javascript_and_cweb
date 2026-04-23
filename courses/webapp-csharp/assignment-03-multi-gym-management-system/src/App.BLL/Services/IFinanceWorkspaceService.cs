using App.DTO.v1.Finance;

namespace App.BLL.Services;

public interface IFinanceWorkspaceService
{
    Task<FinanceWorkspaceResponse> GetCurrentWorkspaceAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<FinanceWorkspaceResponse> GetWorkspaceAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<InvoiceResponse>> GetInvoicesAsync(string gymCode, Guid? memberId, CancellationToken cancellationToken = default);
    Task<InvoiceResponse> GetInvoiceAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<InvoiceResponse> CreateInvoiceAsync(string gymCode, InvoiceCreateRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceResponse> AddInvoicePaymentAsync(string gymCode, Guid id, InvoicePaymentRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceResponse> AddInvoiceRefundAsync(string gymCode, Guid id, InvoicePaymentRequest request, CancellationToken cancellationToken = default);
}
