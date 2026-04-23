using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Finance;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class FinanceWorkspaceService(
    IAppDbContext dbContext,
    IAuthorizationService authorizationService,
    IUserContextService userContextService) : IFinanceWorkspaceService
{
    public async Task<FinanceWorkspaceResponse> GetCurrentWorkspaceAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var currentMember = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken)
                            ?? throw new NotFoundException("Current user does not have a member profile in the active gym.");

        return await BuildWorkspaceAsync(gymId, currentMember.Id, cancellationToken);
    }

    public async Task<FinanceWorkspaceResponse> GetWorkspaceAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member,
            RoleNames.SystemBilling);

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, memberId, cancellationToken);

        return await BuildWorkspaceAsync(gymId, memberId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<InvoiceResponse>> GetInvoicesAsync(string gymCode, Guid? memberId, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var query = dbContext.Invoices
            .Where(entity => entity.GymId == gymId)
            .Include(entity => entity.Member)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.Lines)
            .Include(entity => entity.Payments)
            .AsQueryable();

        if (memberId.HasValue)
        {
            await authorizationService.EnsureMemberSelfAccessAsync(gymId, memberId.Value, cancellationToken);
            query = query.Where(entity => entity.MemberId == memberId.Value);
        }

        if (userContextService.GetCurrent().HasRole(RoleNames.Member))
        {
            var currentMember = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken)
                                ?? throw new NotFoundException("Current user does not have a member profile in the active gym.");
            query = query.Where(entity => entity.MemberId == currentMember.Id);
        }

        var invoices = await query
            .OrderByDescending(entity => entity.DueAtUtc)
            .ToListAsync(cancellationToken);

        return invoices.Select(ToInvoiceResponse).ToArray();
    }

    public async Task<InvoiceResponse> GetInvoiceAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var invoice = await LoadInvoiceAsync(gymId, id, cancellationToken)
                      ?? throw new NotFoundException("Invoice was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, invoice.MemberId, cancellationToken);

        return ToInvoiceResponse(invoice);
    }

    public async Task<InvoiceResponse> CreateInvoiceAsync(string gymCode, InvoiceCreateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        if (!await dbContext.Members.AnyAsync(entity => entity.GymId == gymId && entity.Id == request.MemberId, cancellationToken))
        {
            throw new ValidationAppException("Member was not found in the active gym.");
        }

        var lines = ParseInvoiceLines(gymId, request.Lines);
        var subtotal = lines.Where(entity => !entity.IsCredit).Sum(entity => entity.LineTotal);
        var credits = lines.Where(entity => entity.IsCredit).Sum(entity => entity.LineTotal);
        var total = Math.Max(0m, subtotal - credits);

        var invoice = new Invoice
        {
            GymId = gymId,
            MemberId = request.MemberId,
            InvoiceNumber = await GenerateInvoiceNumberAsync(gymId, cancellationToken),
            IssuedAtUtc = DateTime.UtcNow,
            DueAtUtc = request.DueAtUtc,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "EUR" : request.CurrencyCode.Trim().ToUpperInvariant(),
            SubtotalAmount = subtotal,
            CreditAmount = credits,
            TotalAmount = total,
            PaidAmount = 0m,
            OutstandingAmount = total,
            Status = total == 0m ? InvoiceStatus.Paid : InvoiceStatus.Issued,
            Notes = request.Notes?.Trim(),
            Lines = lines
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadInvoiceAsync(gymId, invoice.Id, cancellationToken)
                    ?? throw new NotFoundException("Invoice was not found after creation.");

        return ToInvoiceResponse(saved);
    }

    public Task<InvoiceResponse> AddInvoicePaymentAsync(string gymCode, Guid id, InvoicePaymentRequest request, CancellationToken cancellationToken = default) =>
        AddInvoiceAdjustmentAsync(gymCode, id, request, false, cancellationToken);

    public Task<InvoiceResponse> AddInvoiceRefundAsync(string gymCode, Guid id, InvoicePaymentRequest request, CancellationToken cancellationToken = default) =>
        AddInvoiceAdjustmentAsync(gymCode, id, request, true, cancellationToken);

    private async Task<InvoiceResponse> AddInvoiceAdjustmentAsync(
        string gymCode,
        Guid id,
        InvoicePaymentRequest request,
        bool isRefund,
        CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var invoice = await dbContext.Invoices
            .Include(entity => entity.Payments)
            .Include(entity => entity.Lines)
            .Include(entity => entity.Member)
                .ThenInclude(entity => entity!.Person)
            .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == id, cancellationToken)
            ?? throw new NotFoundException("Invoice was not found.");

        await authorizationService.EnsureMemberSelfAccessAsync(gymId, invoice.MemberId, cancellationToken);

        if (request.Amount <= 0)
        {
            throw new ValidationAppException("Amount must be greater than zero.");
        }

        if (isRefund && request.Amount > invoice.PaidAmount)
        {
            throw new ValidationAppException("Refund amount cannot exceed paid amount.");
        }

        if (!isRefund && request.Amount > invoice.OutstandingAmount)
        {
            throw new ValidationAppException("Payment amount cannot exceed outstanding amount.");
        }

        var paymentRecord = new Payment
        {
            GymId = gymId,
            MembershipId = null,
            BookingId = null,
            Amount = request.Amount,
            CurrencyCode = invoice.CurrencyCode,
            Reference = request.Reference?.Trim(),
            Status = isRefund ? PaymentStatus.Refunded : PaymentStatus.Completed
        };

        dbContext.Payments.Add(paymentRecord);

        var invoicePayment = new InvoicePayment
        {
            GymId = gymId,
            InvoiceId = invoice.Id,
            Payment = paymentRecord,
            Amount = request.Amount,
            IsRefund = isRefund,
            AppliedAtUtc = DateTime.UtcNow,
            Reference = request.Reference?.Trim(),
            Notes = request.Notes?.Trim()
        };

        dbContext.InvoicePayments.Add(invoicePayment);

        if (isRefund)
        {
            invoice.PaidAmount = Math.Max(0m, invoice.PaidAmount - request.Amount);
            invoice.OutstandingAmount = Math.Min(invoice.TotalAmount, invoice.OutstandingAmount + request.Amount);
        }
        else
        {
            invoice.PaidAmount = Math.Min(invoice.TotalAmount, invoice.PaidAmount + request.Amount);
            invoice.OutstandingAmount = Math.Max(0m, invoice.OutstandingAmount - request.Amount);
        }

        invoice.Status = ResolveInvoiceStatus(invoice);

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadInvoiceAsync(gymId, invoice.Id, cancellationToken)
                    ?? throw new NotFoundException("Invoice was not found after payment update.");

        return ToInvoiceResponse(saved);
    }

    private async Task<FinanceWorkspaceResponse> BuildWorkspaceAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken)
    {
        var member = await dbContext.Members
            .Include(entity => entity.Person)
            .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == memberId, cancellationToken)
            ?? throw new NotFoundException("Member was not found.");

        var invoices = await dbContext.Invoices
            .Where(entity => entity.GymId == gymId && entity.MemberId == memberId)
            .Include(entity => entity.Member)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.Lines)
            .Include(entity => entity.Payments)
            .OrderByDescending(entity => entity.DueAtUtc)
            .ToListAsync(cancellationToken);

        var mappedInvoices = invoices.Select(ToInvoiceResponse).ToArray();
        var paymentHistory = mappedInvoices
            .SelectMany(entity => entity.Payments)
            .OrderByDescending(entity => entity.AppliedAtUtc)
            .ToArray();

        return new FinanceWorkspaceResponse
        {
            MemberId = member.Id,
            MemberName = $"{member.Person?.FirstName} {member.Person?.LastName}".Trim(),
            MemberCode = member.MemberCode,
            OutstandingBalance = mappedInvoices.Sum(entity => entity.OutstandingAmount),
            TotalRefundCredits = paymentHistory.Where(entity => entity.IsRefund).Sum(entity => entity.Amount),
            OverdueInvoiceCount = mappedInvoices.Count(entity => entity.IsOverdue),
            Invoices = mappedInvoices,
            PaymentHistory = paymentHistory
        };
    }

    private async Task<Invoice?> LoadInvoiceAsync(Guid gymId, Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Invoices
            .Where(entity => entity.GymId == gymId && entity.Id == id)
            .Include(entity => entity.Member)
                .ThenInclude(entity => entity!.Person)
            .Include(entity => entity.Lines)
            .Include(entity => entity.Payments)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static List<InvoiceLine> ParseInvoiceLines(Guid gymId, IReadOnlyCollection<InvoiceLineRequest> lines)
    {
        if (lines.Count == 0)
        {
            throw new ValidationAppException("At least one invoice line is required.");
        }

        return lines.Select(line =>
        {
            if (string.IsNullOrWhiteSpace(line.Description))
            {
                throw new ValidationAppException("Invoice line description is required.");
            }

            if (line.Quantity <= 0)
            {
                throw new ValidationAppException("Invoice line quantity must be greater than zero.");
            }

            if (line.UnitPrice < 0)
            {
                throw new ValidationAppException("Invoice line unit price cannot be negative.");
            }

            return new InvoiceLine
            {
                GymId = gymId,
                Description = line.Description.Trim(),
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero),
                IsCredit = line.IsCredit,
                Notes = line.Notes?.Trim()
            };
        }).ToList();
    }

    private async Task<string> GenerateInvoiceNumberAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var dayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var existingTodayCount = await dbContext.Invoices
            .CountAsync(entity => entity.GymId == gymId && entity.IssuedAtUtc >= dayStart && entity.IssuedAtUtc < dayEnd, cancellationToken);

        return $"INV-{today:yyyyMMdd}-{existingTodayCount + 1:D4}";
    }

    private static InvoiceStatus ResolveInvoiceStatus(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return InvoiceStatus.Cancelled;
        }

        if (invoice.PaidAmount <= 0 && invoice.OutstandingAmount > 0 && invoice.DueAtUtc < DateTime.UtcNow)
        {
            return InvoiceStatus.Overdue;
        }

        if (invoice.OutstandingAmount <= 0)
        {
            return InvoiceStatus.Paid;
        }

        if (invoice.PaidAmount > 0)
        {
            return InvoiceStatus.PartiallyPaid;
        }

        return invoice.DueAtUtc < DateTime.UtcNow
            ? InvoiceStatus.Overdue
            : InvoiceStatus.Issued;
    }

    private static InvoiceResponse ToInvoiceResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            MemberId = invoice.MemberId,
            MemberName = $"{invoice.Member?.Person?.FirstName} {invoice.Member?.Person?.LastName}".Trim(),
            InvoiceNumber = invoice.InvoiceNumber,
            IssuedAtUtc = invoice.IssuedAtUtc,
            DueAtUtc = invoice.DueAtUtc,
            CurrencyCode = invoice.CurrencyCode,
            SubtotalAmount = invoice.SubtotalAmount,
            CreditAmount = invoice.CreditAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            OutstandingAmount = invoice.OutstandingAmount,
            IsOverdue = invoice.OutstandingAmount > 0 && invoice.DueAtUtc < DateTime.UtcNow,
            Status = invoice.Status,
            Notes = invoice.Notes,
            Lines = invoice.Lines
                .OrderBy(entity => entity.CreatedAtUtc)
                .Select(entity => new InvoiceLineResponse
                {
                    Id = entity.Id,
                    Description = entity.Description,
                    Quantity = entity.Quantity,
                    UnitPrice = entity.UnitPrice,
                    LineTotal = entity.LineTotal,
                    IsCredit = entity.IsCredit,
                    Notes = entity.Notes
                })
                .ToArray(),
            Payments = invoice.Payments
                .OrderByDescending(entity => entity.AppliedAtUtc)
                .Select(entity => new InvoicePaymentResponse
                {
                    Id = entity.Id,
                    Amount = entity.Amount,
                    IsRefund = entity.IsRefund,
                    AppliedAtUtc = entity.AppliedAtUtc,
                    Reference = entity.Reference,
                    Notes = entity.Notes
                })
                .ToArray()
        };
    }
}
