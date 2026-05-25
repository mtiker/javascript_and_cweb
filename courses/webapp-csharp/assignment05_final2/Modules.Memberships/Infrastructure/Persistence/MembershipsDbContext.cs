using SharedKernel.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Modules.Memberships.Infrastructure.Persistence;

public sealed class MembershipsDbContext(
    DbContextOptions<MembershipsDbContext> options,
    IGymContext gymContext)
    : ModuleDbContextBase<MembershipsDbContext>(options, gymContext)
{
    public DbSet<Person> People { get; set; } = default!;
    public DbSet<Member> Members { get; set; } = default!;
    public DbSet<MembershipPackage> MembershipPackages { get; set; } = default!;
    public DbSet<Membership> Memberships { get; set; } = default!;
    public DbSet<Payment> Payments { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("memberships");

        builder.Entity<Person>(entity =>
        {
            entity.HasIndex(person => person.PersonalCode)
                .IsUnique();

            entity.Ignore(person => person.AppUser);
            entity.Ignore(person => person.Contacts);
            entity.Ignore(person => person.StaffProfiles);
        });

        builder.Entity<Member>(entity =>
        {
            entity.HasOne(member => member.Person)
                .WithMany(person => person.MemberProfiles)
                .HasForeignKey(member => member.PersonId);

            entity.HasIndex(member => new { member.GymId, member.MemberCode })
                .IsUnique();

            entity.HasIndex(member => new { member.GymId, member.PersonId })
                .IsUnique();

            entity.Ignore(member => member.Bookings);
        });

        builder.Entity<MembershipPackage>(entity =>
        {
            entity.Property(package => package.BasePrice)
                .HasPrecision(12, 2);
        });

        builder.Entity<Membership>(entity =>
        {
            entity.HasOne(membership => membership.Member)
                .WithMany(member => member.Memberships)
                .HasForeignKey(membership => membership.MemberId);

            entity.HasOne(membership => membership.MembershipPackage)
                .WithMany(package => package.Memberships)
                .HasForeignKey(membership => membership.MembershipPackageId);

            entity.HasIndex(membership => new { membership.GymId, membership.MemberId, membership.StartDate, membership.EndDate });

            entity.Property(membership => membership.PriceAtPurchase)
                .HasPrecision(12, 2);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.HasOne(payment => payment.Membership)
                .WithMany(membership => membership.Payments)
                .HasForeignKey(payment => payment.MembershipId);

            entity.Ignore(payment => payment.Booking);

            entity.Property(payment => payment.Amount)
                .HasPrecision(12, 2);
        });

        ConfigureTenantSoftDeleteFilter<Member>(builder);
        ConfigureTenantSoftDeleteFilter<MembershipPackage>(builder);
        ConfigureTenantSoftDeleteFilter<Membership>(builder);
        ConfigureTenantSoftDeleteFilter<Payment>(builder);
        ConfigureModuleDefaults(builder);
    }
}
