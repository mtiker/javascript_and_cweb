using SharedKernel.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Modules.Training.Infrastructure.Persistence;

public sealed class TrainingDbContext(
    DbContextOptions<TrainingDbContext> options,
    IGymContext gymContext)
    : ModuleDbContextBase<TrainingDbContext>(options, gymContext)
{
    public DbSet<Person> People { get; set; } = default!;
    public DbSet<Staff> Staff { get; set; } = default!;
    public DbSet<TrainingCategory> TrainingCategories { get; set; } = default!;
    public DbSet<TrainingSession> TrainingSessions { get; set; } = default!;
    public DbSet<Booking> Bookings { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("training");

        builder.Entity<Person>(entity =>
        {
            entity.HasIndex(person => person.PersonalCode)
                .IsUnique();

            entity.Ignore(person => person.AppUser);
            entity.Ignore(person => person.Contacts);
            entity.Ignore(person => person.MemberProfiles);
        });

        builder.Entity<Staff>(entity =>
        {
            entity.HasOne(staff => staff.Person)
                .WithMany(person => person.StaffProfiles)
                .HasForeignKey(staff => staff.PersonId);

            entity.HasIndex(staff => new { staff.GymId, staff.StaffCode })
                .IsUnique();

            entity.HasIndex(staff => new { staff.GymId, staff.PersonId })
                .IsUnique();

            entity.Ignore(staff => staff.AssignedTasks);
            entity.Ignore(staff => staff.CreatedTasks);
        });

        builder.Entity<TrainingSession>(entity =>
        {
            entity.HasOne(session => session.Category)
                .WithMany(category => category.Sessions)
                .HasForeignKey(session => session.CategoryId);

            entity.HasOne(session => session.TrainerStaff)
                .WithMany()
                .HasForeignKey(session => session.TrainerStaffId);

            entity.HasIndex(session => new { session.GymId, session.StartAtUtc, session.EndAtUtc });

            entity.Property(session => session.BasePrice)
                .HasPrecision(12, 2);
        });

        builder.Entity<Booking>(entity =>
        {
            entity.HasOne(booking => booking.TrainingSession)
                .WithMany(session => session.Bookings)
                .HasForeignKey(booking => booking.TrainingSessionId);

            entity.Ignore(booking => booking.Member);
            entity.Ignore(booking => booking.Payments);

            entity.HasIndex(booking => new { booking.GymId, booking.MemberId, booking.TrainingSessionId })
                .IsUnique();

            entity.Property(booking => booking.ChargedPrice)
                .HasPrecision(12, 2);
        });

        ConfigureTenantSoftDeleteFilter<Staff>(builder);
        ConfigureTenantSoftDeleteFilter<TrainingCategory>(builder);
        ConfigureTenantSoftDeleteFilter<TrainingSession>(builder);
        ConfigureTenantSoftDeleteFilter<Booking>(builder);
        ConfigureModuleDefaults(builder);
    }
}
