using App.Domain.Entities;
using App.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasOne(user => user.Person)
            .WithOne(person => person.AppUser)
            .HasForeignKey<AppUser>(user => user.PersonId);

        builder.Property(user => user.DisplayName)
            .HasMaxLength(128);

        builder.HasIndex(user => user.PersonId)
            .IsUnique();
    }
}

public sealed class AppRefreshTokenConfiguration : IEntityTypeConfiguration<AppRefreshToken>
{
    public void Configure(EntityTypeBuilder<AppRefreshToken> builder)
    {
        builder.HasIndex(token => token.RefreshToken)
            .IsUnique();
    }
}

public sealed class GymConfiguration : IEntityTypeConfiguration<Gym>
{
    public void Configure(EntityTypeBuilder<Gym> builder)
    {
        builder.HasIndex(gym => gym.Code)
            .IsUnique();

        builder.Property(gym => gym.Name)
            .HasMaxLength(128);

        builder.Property(gym => gym.Code)
            .HasMaxLength(64);
    }
}

public sealed class GymSettingsConfiguration : IEntityTypeConfiguration<GymSettings>
{
    public void Configure(EntityTypeBuilder<GymSettings> builder)
    {
        builder.HasIndex(settings => settings.GymId)
            .IsUnique();

        builder.HasOne(settings => settings.Gym)
            .WithOne(gym => gym.Settings)
            .HasForeignKey<GymSettings>(settings => settings.GymId);
    }
}

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasOne(subscription => subscription.Gym)
            .WithMany(gym => gym.Subscriptions)
            .HasForeignKey(subscription => subscription.GymId);

        builder.Property(subscription => subscription.MonthlyPrice)
            .HasPrecision(12, 2);
    }
}

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.HasOne(ticket => ticket.Gym)
            .WithMany(gym => gym.SupportTickets)
            .HasForeignKey(ticket => ticket.GymId);
    }
}

public sealed class AppUserGymRoleConfiguration : IEntityTypeConfiguration<AppUserGymRole>
{
    public void Configure(EntityTypeBuilder<AppUserGymRole> builder)
    {
        builder.HasIndex(link => new { link.AppUserId, link.GymId, link.RoleName })
            .IsUnique();

        builder.HasOne(link => link.AppUser)
            .WithMany(user => user.GymRoles)
            .HasForeignKey(link => link.AppUserId);

        builder.HasOne(link => link.Gym)
            .WithMany(gym => gym.UserRoles)
            .HasForeignKey(link => link.GymId);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasIndex(log => new { log.GymId, log.ChangedAtUtc });
    }
}
