using App.Domain.Entities;
using App.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Infrastructure;

public interface IAppDbContext
{
    DbSet<AppRefreshToken> RefreshTokens { get; }
    DbSet<Gym> Gyms { get; }
    DbSet<GymSettings> GymSettings { get; }
    DbSet<AppUserGymRole> AppUserGymRoles { get; }
    DbSet<AppUser> Users { get; }
    DbSet<Person> People { get; }
    DbSet<Contact> Contacts { get; }
    DbSet<PersonContact> PersonContacts { get; }
    DbSet<GymContact> GymContacts { get; }
    DbSet<Member> Members { get; }
    DbSet<Staff> Staff { get; }
    DbSet<TrainingCategory> TrainingCategories { get; }
    DbSet<TrainingSession> TrainingSessions { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<MembershipPackage> MembershipPackages { get; }
    DbSet<Membership> Memberships { get; }
    DbSet<Payment> Payments { get; }
    DbSet<EquipmentModel> EquipmentModels { get; }
    DbSet<Equipment> Equipment { get; }
    DbSet<MaintenanceTask> MaintenanceTasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
