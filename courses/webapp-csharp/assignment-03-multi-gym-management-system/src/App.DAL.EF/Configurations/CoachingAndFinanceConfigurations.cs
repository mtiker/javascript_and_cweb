using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public sealed class CoachingPlanConfiguration : IEntityTypeConfiguration<CoachingPlan>
{
    public void Configure(EntityTypeBuilder<CoachingPlan> builder)
    {
        builder.HasOne(plan => plan.Member)
            .WithMany(member => member.CoachingPlans)
            .HasForeignKey(plan => plan.MemberId);

        builder.HasOne(plan => plan.TrainerStaff)
            .WithMany(staff => staff.CoachingPlans)
            .HasForeignKey(plan => plan.TrainerStaffId);

        builder.HasOne(plan => plan.CreatedByStaff)
            .WithMany()
            .HasForeignKey(plan => plan.CreatedByStaffId);

        builder.HasIndex(plan => new { plan.GymId, plan.MemberId, plan.Status, plan.CreatedAtUtc });
    }
}

public sealed class CoachingPlanItemConfiguration : IEntityTypeConfiguration<CoachingPlanItem>
{
    public void Configure(EntityTypeBuilder<CoachingPlanItem> builder)
    {
        builder.HasOne(item => item.CoachingPlan)
            .WithMany(plan => plan.Items)
            .HasForeignKey(item => item.CoachingPlanId);

        builder.HasOne(item => item.DecisionByStaff)
            .WithMany(staff => staff.CoachingPlanItemDecisions)
            .HasForeignKey(item => item.DecisionByStaffId);

        builder.HasIndex(item => new { item.GymId, item.CoachingPlanId, item.Sequence })
            .IsUnique();
    }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasOne(invoice => invoice.Member)
            .WithMany(member => member.Invoices)
            .HasForeignKey(invoice => invoice.MemberId);

        builder.HasIndex(invoice => new { invoice.GymId, invoice.InvoiceNumber })
            .IsUnique();

        builder.HasIndex(invoice => new { invoice.GymId, invoice.MemberId, invoice.DueAtUtc, invoice.Status });

        builder.Property(invoice => invoice.SubtotalAmount)
            .HasPrecision(12, 2);

        builder.Property(invoice => invoice.CreditAmount)
            .HasPrecision(12, 2);

        builder.Property(invoice => invoice.TotalAmount)
            .HasPrecision(12, 2);

        builder.Property(invoice => invoice.PaidAmount)
            .HasPrecision(12, 2);

        builder.Property(invoice => invoice.OutstandingAmount)
            .HasPrecision(12, 2);
    }
}

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.HasOne(line => line.Invoice)
            .WithMany(invoice => invoice.Lines)
            .HasForeignKey(line => line.InvoiceId);

        builder.Property(line => line.Quantity)
            .HasPrecision(12, 2);

        builder.Property(line => line.UnitPrice)
            .HasPrecision(12, 2);

        builder.Property(line => line.LineTotal)
            .HasPrecision(12, 2);
    }
}

public sealed class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.HasOne(invoicePayment => invoicePayment.Invoice)
            .WithMany(invoice => invoice.Payments)
            .HasForeignKey(invoicePayment => invoicePayment.InvoiceId);

        builder.HasOne(invoicePayment => invoicePayment.Payment)
            .WithMany()
            .HasForeignKey(invoicePayment => invoicePayment.PaymentId);

        builder.Property(invoicePayment => invoicePayment.Amount)
            .HasPrecision(12, 2);
    }
}
