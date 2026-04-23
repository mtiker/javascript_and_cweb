using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public sealed class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> builder)
    {
        builder.HasOne(session => session.Category)
            .WithMany(category => category.Sessions)
            .HasForeignKey(session => session.CategoryId);

        builder.HasIndex(session => new { session.GymId, session.StartAtUtc, session.EndAtUtc });

        builder.Property(session => session.BasePrice)
            .HasPrecision(12, 2);
    }
}

public sealed class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
{
    public void Configure(EntityTypeBuilder<WorkShift> builder)
    {
        builder.HasOne(shift => shift.Contract)
            .WithMany(contract => contract.WorkShifts)
            .HasForeignKey(shift => shift.ContractId);

        builder.HasOne(shift => shift.TrainingSession)
            .WithMany(session => session.WorkShifts)
            .HasForeignKey(shift => shift.TrainingSessionId);

        builder.HasIndex(shift => new { shift.GymId, shift.ContractId, shift.StartAtUtc, shift.EndAtUtc });
    }
}

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasOne(booking => booking.TrainingSession)
            .WithMany(session => session.Bookings)
            .HasForeignKey(booking => booking.TrainingSessionId);

        builder.HasOne(booking => booking.Member)
            .WithMany(member => member.Bookings)
            .HasForeignKey(booking => booking.MemberId);

        builder.HasIndex(booking => new { booking.GymId, booking.MemberId, booking.TrainingSessionId })
            .IsUnique();

        builder.Property(booking => booking.ChargedPrice)
            .HasPrecision(12, 2);
    }
}

public sealed class MembershipPackageConfiguration : IEntityTypeConfiguration<MembershipPackage>
{
    public void Configure(EntityTypeBuilder<MembershipPackage> builder)
    {
        builder.Property(package => package.BasePrice)
            .HasPrecision(12, 2);
    }
}

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.HasOne(membership => membership.Member)
            .WithMany(member => member.Memberships)
            .HasForeignKey(membership => membership.MemberId);

        builder.HasOne(membership => membership.MembershipPackage)
            .WithMany(package => package.Memberships)
            .HasForeignKey(membership => membership.MembershipPackageId);

        builder.HasIndex(membership => new { membership.GymId, membership.MemberId, membership.StartDate, membership.EndDate });

        builder.Property(membership => membership.PriceAtPurchase)
            .HasPrecision(12, 2);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasOne(payment => payment.Membership)
            .WithMany(membership => membership.Payments)
            .HasForeignKey(payment => payment.MembershipId);

        builder.HasOne(payment => payment.Booking)
            .WithMany(booking => booking.Payments)
            .HasForeignKey(payment => payment.BookingId);

        builder.Property(payment => payment.Amount)
            .HasPrecision(12, 2);
    }
}
