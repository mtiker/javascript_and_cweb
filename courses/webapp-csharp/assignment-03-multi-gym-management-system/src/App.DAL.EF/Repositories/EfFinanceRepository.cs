using App.BLL.Contracts.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class EfFinanceRepository(AppDbContext dbContext) : IFinanceRepository
{
    public async Task<IReadOnlyList<Invoice>> ListInvoicesAsync(Guid gymId, Guid? memberId, CancellationToken cancellationToken = default)
    {
        var query = BaseInvoiceQuery(gymId);

        if (memberId.HasValue)
        {
            query = query.Where(invoice => invoice.MemberId == memberId.Value);
        }

        return await query
            .OrderByDescending(invoice => invoice.DueAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Invoice?> FindInvoiceAsync(Guid gymId, Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return BaseInvoiceQuery(gymId)
            .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken);
    }

    public Task<int> CountInvoicesIssuedBetweenAsync(Guid gymId, DateTime fromUtc, DateTime untilUtc, CancellationToken cancellationToken = default)
    {
        return dbContext.Invoices.CountAsync(
            invoice => invoice.GymId == gymId && invoice.IssuedAtUtc >= fromUtc && invoice.IssuedAtUtc < untilUtc,
            cancellationToken);
    }

    public async Task AddInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        await dbContext.Invoices.AddAsync(invoice, cancellationToken);
    }

    public async Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        await dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task AddInvoicePaymentAsync(InvoicePayment invoicePayment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoicePayment);
        await dbContext.InvoicePayments.AddAsync(invoicePayment, cancellationToken);
    }

    private IQueryable<Invoice> BaseInvoiceQuery(Guid gymId)
    {
        return dbContext.Invoices
            .Where(invoice => invoice.GymId == gymId)
            .Include(invoice => invoice.Member)
                .ThenInclude(member => member!.Person)
            .Include(invoice => invoice.Lines)
            .Include(invoice => invoice.Payments);
    }
}
