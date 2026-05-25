using App.Domain.Entities;
using App.Domain.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Modules.Users.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    public DbSet<AppRefreshToken> RefreshTokens { get; set; } = default!;
    public DbSet<Person> People { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("users");

        builder.Entity<AppUser>(entity =>
        {
            entity.HasOne(user => user.Person)
                .WithOne(person => person.AppUser)
                .HasForeignKey<AppUser>(user => user.PersonId);

            entity.Property(user => user.DisplayName)
                .HasMaxLength(128);

            entity.HasIndex(user => user.PersonId)
                .IsUnique();

            entity.Ignore(user => user.GymRoles);
        });

        builder.Entity<AppRefreshToken>(entity =>
        {
            entity.HasIndex(token => token.RefreshToken)
                .IsUnique();
        });

        builder.Entity<Person>(entity =>
        {
            entity.HasIndex(person => person.PersonalCode)
                .IsUnique();

            entity.Ignore(person => person.Contacts);
            entity.Ignore(person => person.MemberProfiles);
            entity.Ignore(person => person.StaffProfiles);
        });

        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
