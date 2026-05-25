using SharedKernel.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Modules.Gyms.Infrastructure.Persistence;

public sealed class GymsDbContext(
    DbContextOptions<GymsDbContext> options,
    IGymContext gymContext)
    : ModuleDbContextBase<GymsDbContext>(options, gymContext)
{
    public DbSet<Gym> Gyms { get; set; } = default!;
    public DbSet<GymSettings> GymSettings { get; set; } = default!;
    public DbSet<AppUserGymRole> AppUserGymRoles { get; set; } = default!;
    public DbSet<GymContact> GymContacts { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("gyms");

        builder.Entity<Gym>(entity =>
        {
            entity.HasIndex(gym => gym.Code)
                .IsUnique();

            entity.Property(gym => gym.Name)
                .HasMaxLength(128);

            entity.Property(gym => gym.Code)
                .HasMaxLength(64);

            entity.Ignore(gym => gym.Contacts);
        });

        builder.Entity<GymSettings>(entity =>
        {
            entity.HasIndex(settings => settings.GymId)
                .IsUnique();

            entity.HasOne(settings => settings.Gym)
                .WithOne(gym => gym.Settings)
                .HasForeignKey<GymSettings>(settings => settings.GymId);
        });

        builder.Entity<AppUserGymRole>(entity =>
        {
            entity.HasIndex(link => new { link.AppUserId, link.GymId, link.RoleName })
                .IsUnique();

            entity.Ignore(link => link.AppUser);

            entity.HasOne(link => link.Gym)
                .WithMany(gym => gym.UserRoles)
                .HasForeignKey(link => link.GymId);
        });

        builder.Entity<GymContact>(entity =>
        {
            entity.HasIndex(link => new { link.GymId, link.ContactId })
                .IsUnique();

            entity.Ignore(link => link.Contact);
        });

        ConfigureTenantFilter<GymSettings>(builder);
        ConfigureTenantFilter<AppUserGymRole>(builder);
        ConfigureTenantFilter<GymContact>(builder);
        ConfigureModuleDefaults(builder);
    }
}
