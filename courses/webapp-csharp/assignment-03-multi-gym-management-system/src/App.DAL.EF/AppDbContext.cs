using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using App.BLL.Contracts.Infrastructure;
using App.DAL.EF.Tenant;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace App.DAL.EF;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IGymContext gymContext,
    IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IDataProtectionKeyContext, IAppDbContext
{
    private readonly IGymContext _gymContext = gymContext;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private bool _savingAuditLog;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    public DbSet<AppRefreshToken> RefreshTokens { get; set; } = default!;

    public DbSet<Gym> Gyms { get; set; } = default!;
    public DbSet<GymSettings> GymSettings { get; set; } = default!;
    public DbSet<Subscription> Subscriptions { get; set; } = default!;
    public DbSet<SupportTicket> SupportTickets { get; set; } = default!;
    public DbSet<AuditLog> AuditLogs { get; set; } = default!;
    public DbSet<AppUserGymRole> AppUserGymRoles { get; set; } = default!;

    public DbSet<Person> People { get; set; } = default!;
    public DbSet<Contact> Contacts { get; set; } = default!;
    public DbSet<PersonContact> PersonContacts { get; set; } = default!;
    public DbSet<GymContact> GymContacts { get; set; } = default!;
    public DbSet<Member> Members { get; set; } = default!;
    public DbSet<Staff> Staff { get; set; } = default!;
    public DbSet<JobRole> JobRoles { get; set; } = default!;
    public DbSet<EmploymentContract> EmploymentContracts { get; set; } = default!;
    public DbSet<Vacation> Vacations { get; set; } = default!;

    public DbSet<TrainingCategory> TrainingCategories { get; set; } = default!;
    public DbSet<TrainingSession> TrainingSessions { get; set; } = default!;
    public DbSet<WorkShift> WorkShifts { get; set; } = default!;
    public DbSet<Booking> Bookings { get; set; } = default!;
    public DbSet<MembershipPackage> MembershipPackages { get; set; } = default!;
    public DbSet<Membership> Memberships { get; set; } = default!;
    public DbSet<Payment> Payments { get; set; } = default!;
    public DbSet<OpeningHours> OpeningHours { get; set; } = default!;
    public DbSet<OpeningHoursException> OpeningHoursExceptions { get; set; } = default!;
    public DbSet<EquipmentModel> EquipmentModels { get; set; } = default!;
    public DbSet<Equipment> Equipment { get; set; } = default!;
    public DbSet<MaintenanceTask> MaintenanceTasks { get; set; } = default!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        ApplySoftDelete();

        var auditLogEntries = BuildAuditLogEntries();
        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditLogEntries.Count > 0 && !_savingAuditLog)
        {
            _savingAuditLog = true;
            AuditLogs.AddRange(auditLogEntries);
            await base.SaveChangesAsync(cancellationToken);
            _savingAuditLog = false;
        }

        return result;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<LangStr>()
            .HaveConversion<LangStrValueConverter, LangStrValueComparer>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<LangStr>();
        ConfigureDateTimeAsUtc(builder);
        ConfigureLangStrAsJsonb(builder);

        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        builder.Entity<AppUser>().HasOne(user => user.Person).WithOne(person => person.AppUser).HasForeignKey<AppUser>(user => user.PersonId);
        builder.Entity<AppUser>().Property(user => user.DisplayName).HasMaxLength(128);
        builder.Entity<AppUser>().HasIndex(user => user.PersonId).IsUnique();

        builder.Entity<AppRefreshToken>().HasIndex(token => token.RefreshToken).IsUnique();

        builder.Entity<Gym>().HasIndex(gym => gym.Code).IsUnique();
        builder.Entity<Gym>().Property(gym => gym.Name).HasMaxLength(128);
        builder.Entity<Gym>().Property(gym => gym.Code).HasMaxLength(64);
        builder.Entity<GymSettings>().HasIndex(settings => settings.GymId).IsUnique();
        builder.Entity<GymSettings>().HasOne(settings => settings.Gym).WithOne(gym => gym.Settings).HasForeignKey<GymSettings>(settings => settings.GymId);
        builder.Entity<Subscription>().HasOne(subscription => subscription.Gym).WithMany(gym => gym.Subscriptions).HasForeignKey(subscription => subscription.GymId);
        builder.Entity<SupportTicket>().HasOne(ticket => ticket.Gym).WithMany(gym => gym.SupportTickets).HasForeignKey(ticket => ticket.GymId);
        builder.Entity<AppUserGymRole>().HasIndex(link => new { link.AppUserId, link.GymId, link.RoleName }).IsUnique();
        builder.Entity<AppUserGymRole>().HasOne(link => link.AppUser).WithMany(user => user.GymRoles).HasForeignKey(link => link.AppUserId);
        builder.Entity<AppUserGymRole>().HasOne(link => link.Gym).WithMany(gym => gym.UserRoles).HasForeignKey(link => link.GymId);
        builder.Entity<AuditLog>().HasIndex(log => new { log.GymId, log.ChangedAtUtc });

        builder.Entity<Person>().HasIndex(person => person.PersonalCode).IsUnique();
        builder.Entity<Contact>().HasIndex(contact => new { contact.Type, contact.Value }).IsUnique();
        builder.Entity<PersonContact>().HasIndex(link => new { link.PersonId, link.ContactId }).IsUnique();
        builder.Entity<GymContact>().HasIndex(link => new { link.GymId, link.ContactId }).IsUnique();
        builder.Entity<Member>().HasOne(entity => entity.Person).WithMany(entity => entity.MemberProfiles).HasForeignKey(entity => entity.PersonId);
        builder.Entity<Member>().HasIndex(member => new { member.GymId, member.MemberCode }).IsUnique();
        builder.Entity<Member>().HasIndex(member => new { member.GymId, member.PersonId }).IsUnique();
        builder.Entity<Staff>().HasOne(entity => entity.Person).WithMany(entity => entity.StaffProfiles).HasForeignKey(entity => entity.PersonId);
        builder.Entity<Staff>().HasIndex(staff => new { staff.GymId, staff.StaffCode }).IsUnique();
        builder.Entity<Staff>().HasIndex(staff => new { staff.GymId, staff.PersonId }).IsUnique();
        builder.Entity<JobRole>().HasIndex(role => new { role.GymId, role.Code }).IsUnique();
        builder.Entity<EmploymentContract>().HasOne(entity => entity.Staff).WithMany(entity => entity.Contracts).HasForeignKey(entity => entity.StaffId);
        builder.Entity<EmploymentContract>().HasOne(entity => entity.PrimaryJobRole).WithMany(entity => entity.Contracts).HasForeignKey(entity => entity.PrimaryJobRoleId);
        builder.Entity<EmploymentContract>().HasIndex(contract => new { contract.GymId, contract.StaffId, contract.StartDate });
        builder.Entity<Vacation>().HasOne(entity => entity.Contract).WithMany(entity => entity.Vacations).HasForeignKey(entity => entity.ContractId);
        builder.Entity<Vacation>().HasIndex(vacation => new { vacation.GymId, vacation.ContractId, vacation.StartDate, vacation.EndDate });

        builder.Entity<TrainingSession>().HasOne(entity => entity.Category).WithMany(entity => entity.Sessions).HasForeignKey(entity => entity.CategoryId);
        builder.Entity<TrainingSession>().HasIndex(session => new { session.GymId, session.StartAtUtc, session.EndAtUtc });
        builder.Entity<WorkShift>().HasOne(entity => entity.Contract).WithMany(entity => entity.WorkShifts).HasForeignKey(entity => entity.ContractId);
        builder.Entity<WorkShift>().HasOne(entity => entity.TrainingSession).WithMany(entity => entity.WorkShifts).HasForeignKey(entity => entity.TrainingSessionId);
        builder.Entity<WorkShift>().HasIndex(shift => new { shift.GymId, shift.ContractId, shift.StartAtUtc, shift.EndAtUtc });
        builder.Entity<Booking>().HasOne(entity => entity.TrainingSession).WithMany(entity => entity.Bookings).HasForeignKey(entity => entity.TrainingSessionId);
        builder.Entity<Booking>().HasOne(entity => entity.Member).WithMany(entity => entity.Bookings).HasForeignKey(entity => entity.MemberId);
        builder.Entity<Booking>().HasIndex(booking => new { booking.GymId, booking.MemberId, booking.TrainingSessionId }).IsUnique();
        builder.Entity<Membership>().HasOne(entity => entity.Member).WithMany(entity => entity.Memberships).HasForeignKey(entity => entity.MemberId);
        builder.Entity<Membership>().HasOne(entity => entity.MembershipPackage).WithMany(entity => entity.Memberships).HasForeignKey(entity => entity.MembershipPackageId);
        builder.Entity<Membership>().HasIndex(membership => new { membership.GymId, membership.MemberId, membership.StartDate, membership.EndDate });
        builder.Entity<Payment>().HasOne(entity => entity.Membership).WithMany(entity => entity.Payments).HasForeignKey(entity => entity.MembershipId);
        builder.Entity<Payment>().HasOne(entity => entity.Booking).WithMany(entity => entity.Payments).HasForeignKey(entity => entity.BookingId);
        builder.Entity<OpeningHours>().HasIndex(hours => new { hours.GymId, hours.Weekday, hours.ValidFrom, hours.ValidTo });
        builder.Entity<OpeningHoursException>().HasIndex(exception => new { exception.GymId, exception.ExceptionDate }).IsUnique();
        builder.Entity<Equipment>().HasOne(entity => entity.EquipmentModel).WithMany(entity => entity.EquipmentItems).HasForeignKey(entity => entity.EquipmentModelId);
        builder.Entity<Equipment>().HasIndex(item => new { item.GymId, item.AssetTag }).IsUnique();
        builder.Entity<Equipment>().HasIndex(item => new { item.GymId, item.SerialNumber }).IsUnique();
        builder.Entity<MaintenanceTask>().HasOne(entity => entity.Equipment).WithMany(entity => entity.MaintenanceTasks).HasForeignKey(entity => entity.EquipmentId);
        builder.Entity<MaintenanceTask>().HasOne(entity => entity.AssignedStaff).WithMany(entity => entity.AssignedTasks).HasForeignKey(entity => entity.AssignedStaffId);
        builder.Entity<MaintenanceTask>().HasOne(entity => entity.CreatedByStaff).WithMany(entity => entity.CreatedTasks).HasForeignKey(entity => entity.CreatedByStaffId);
        builder.Entity<MaintenanceTask>().HasIndex(task => new { task.GymId, task.EquipmentId, task.Status, task.DueAtUtc });

        builder.Entity<EmploymentContract>().Property(contract => contract.WorkloadPercent).HasPrecision(5, 2);
        builder.Entity<TrainingSession>().Property(session => session.BasePrice).HasPrecision(12, 2);
        builder.Entity<Booking>().Property(booking => booking.ChargedPrice).HasPrecision(12, 2);
        builder.Entity<MembershipPackage>().Property(package => package.BasePrice).HasPrecision(12, 2);
        builder.Entity<Membership>().Property(membership => membership.PriceAtPurchase).HasPrecision(12, 2);
        builder.Entity<Payment>().Property(payment => payment.Amount).HasPrecision(12, 2);
        builder.Entity<Subscription>().Property(subscription => subscription.MonthlyPrice).HasPrecision(12, 2);

        ConfigureTenantFilter<GymSettings>(builder);
        ConfigureTenantFilter<Subscription>(builder);
        ConfigureTenantFilter<SupportTicket>(builder);
        ConfigureTenantFilter<AppUserGymRole>(builder);
        ConfigureTenantFilter<GymContact>(builder);

        ConfigureTenantSoftDeleteFilter<Member>(builder);
        ConfigureTenantSoftDeleteFilter<Staff>(builder);
        ConfigureTenantSoftDeleteFilter<JobRole>(builder);
        ConfigureTenantSoftDeleteFilter<EmploymentContract>(builder);
        ConfigureTenantSoftDeleteFilter<Vacation>(builder);
        ConfigureTenantSoftDeleteFilter<TrainingCategory>(builder);
        ConfigureTenantSoftDeleteFilter<TrainingSession>(builder);
        ConfigureTenantSoftDeleteFilter<WorkShift>(builder);
        ConfigureTenantSoftDeleteFilter<Booking>(builder);
        ConfigureTenantSoftDeleteFilter<MembershipPackage>(builder);
        ConfigureTenantSoftDeleteFilter<Membership>(builder);
        ConfigureTenantSoftDeleteFilter<Payment>(builder);
        ConfigureTenantSoftDeleteFilter<OpeningHours>(builder);
        ConfigureTenantSoftDeleteFilter<OpeningHoursException>(builder);
        ConfigureTenantSoftDeleteFilter<EquipmentModel>(builder);
        ConfigureTenantSoftDeleteFilter<Equipment>(builder);
        ConfigureTenantSoftDeleteFilter<MaintenanceTask>(builder);
    }

    private sealed class LangStrValueConverter() : ValueConverter<LangStr, string>(
        value => JsonSerializer.Serialize(value ?? new LangStr(), JsonSerializerOptions.Web),
        value => string.IsNullOrWhiteSpace(value)
            ? new LangStr()
            : JsonSerializer.Deserialize<LangStr>(value, JsonSerializerOptions.Web) ?? new LangStr());

    private sealed class LangStrValueComparer() : ValueComparer<LangStr>(
        (left, right) => JsonSerializer.Serialize(left ?? new LangStr(), JsonSerializerOptions.Web) ==
                         JsonSerializer.Serialize(right ?? new LangStr(), JsonSerializerOptions.Web),
        value => JsonSerializer.Serialize(value ?? new LangStr(), JsonSerializerOptions.Web).GetHashCode(),
        value => JsonSerializer.Deserialize<LangStr>(
                     JsonSerializer.Serialize(value ?? new LangStr(), JsonSerializerOptions.Web),
                     JsonSerializerOptions.Web)
                 ?? new LangStr());

    private void ConfigureTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(entity =>
            _gymContext.IgnoreGymFilter || entity.GymId == _gymContext.GymId);
    }

    private void ConfigureTenantSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity, ISoftDeleteEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(entity =>
            (_gymContext.IgnoreGymFilter || entity.GymId == _gymContext.GymId) && !entity.IsDeleted);
    }

    private void ApplyAuditFields()
    {
        var nowUtc = DateTime.UtcNow;
        var currentUserId = GetCurrentUserId();

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = nowUtc;
                entry.Entity.CreatedByUserId ??= currentUserId;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = nowUtc;
                entry.Entity.ModifiedByUserId = currentUserId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(entry => entry.State == EntityState.Added))
        {
            if (entry.Entity.GymId == Guid.Empty && _gymContext.GymId.HasValue)
            {
                entry.Entity.GymId = _gymContext.GymId.Value;
            }
        }
    }

    private void ApplySoftDelete()
    {
        var nowUtc = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ISoftDeleteEntity>().Where(entry => entry.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = nowUtc;
        }
    }

    private List<AuditLog> BuildAuditLogEntries()
    {
        var currentUserId = GetCurrentUserId();
        var nowUtc = DateTime.UtcNow;

        var changedEntries = ChangeTracker.Entries()
            .Where(entry =>
                entry.Entity is not AuditLog &&
                entry.Entity is IBaseEntity &&
                entry.Entity is ITenantEntity &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var logs = new List<AuditLog>(changedEntries.Count);

        foreach (var entry in changedEntries)
        {
            var entity = (IBaseEntity)entry.Entity;
            var tenant = (ITenantEntity)entry.Entity;

            logs.Add(new AuditLog
            {
                ActorUserId = currentUserId,
                GymId = tenant.GymId,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = entity.Id,
                Action = entry.State.ToString(),
                ChangedAtUtc = nowUtc,
                ChangesJson = SerializeChangeSet(entry)
            });
        }

        return logs;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private static string SerializeChangeSet(EntityEntry entry)
    {
        var snapshot = entry.Properties.ToDictionary(
            property => property.Metadata.Name,
            property => new
            {
                OldValue = entry.State == EntityState.Added ? null : property.OriginalValue,
                NewValue = entry.State == EntityState.Deleted ? null : property.CurrentValue
            });

        return JsonSerializer.Serialize(snapshot);
    }

    private static void ConfigureDateTimeAsUtc(ModelBuilder builder)
    {
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            value => value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            value => value.HasValue
                ? (value.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                    : value.Value.ToUniversalTime())
                : value,
            value => value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : value);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }

    private static void ConfigureLangStrAsJsonb(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties().Where(property => property.ClrType == typeof(LangStr)))
            {
                property.SetColumnType("jsonb");
            }
        }
    }
}
