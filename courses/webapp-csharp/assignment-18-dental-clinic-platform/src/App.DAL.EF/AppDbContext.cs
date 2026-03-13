using System.Security.Claims;
using System.Text.Json;
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
    ITenantProvider tenantProvider,
    IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IDataProtectionKeyContext
{
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private bool _isSavingAuditLog;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    public DbSet<AppRefreshToken> RefreshTokens { get; set; } = default!;

    public DbSet<Company> Companies { get; set; } = default!;
    public DbSet<CompanySettings> CompanySettings { get; set; } = default!;
    public DbSet<Subscription> Subscriptions { get; set; } = default!;
    public DbSet<AppUserRole> AppUserRoles { get; set; } = default!;

    public DbSet<Patient> Patients { get; set; } = default!;
    public DbSet<ToothRecord> ToothRecords { get; set; } = default!;
    public DbSet<TreatmentType> TreatmentTypes { get; set; } = default!;
    public DbSet<Treatment> Treatments { get; set; } = default!;
    public DbSet<Appointment> Appointments { get; set; } = default!;
    public DbSet<TreatmentPlan> TreatmentPlans { get; set; } = default!;
    public DbSet<PlanItem> PlanItems { get; set; } = default!;
    public DbSet<Xray> Xrays { get; set; } = default!;
    public DbSet<InsurancePlan> InsurancePlans { get; set; } = default!;
    public DbSet<CostEstimate> CostEstimates { get; set; } = default!;
    public DbSet<Invoice> Invoices { get; set; } = default!;
    public DbSet<PaymentPlan> PaymentPlans { get; set; } = default!;
    public DbSet<Dentist> Dentists { get; set; } = default!;
    public DbSet<TreatmentRoom> TreatmentRooms { get; set; } = default!;

    public DbSet<AuditLog> AuditLogs { get; set; } = default!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        ApplySoftDelete();

        var auditLogs = BuildAuditLogEntries();
        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditLogs.Count > 0 && !_isSavingAuditLog)
        {
            _isSavingAuditLog = true;
            AuditLogs.AddRange(auditLogs);
            await base.SaveChangesAsync(cancellationToken);
            _isSavingAuditLog = false;
        }

        return result;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureDateTimeAsUtc(builder);

        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        builder.Entity<Company>().HasIndex(entity => entity.Slug).IsUnique();
        builder.Entity<Company>().Property(entity => entity.Name).HasMaxLength(128);
        builder.Entity<Company>().Property(entity => entity.Slug).HasMaxLength(64);

        builder.Entity<CompanySettings>()
            .HasIndex(entity => entity.CompanyId)
            .IsUnique();
        builder.Entity<CompanySettings>()
            .HasOne(entity => entity.Company)
            .WithOne(entity => entity.Settings)
            .HasForeignKey<CompanySettings>(entity => entity.CompanyId);

        builder.Entity<Subscription>()
            .HasOne(entity => entity.Company)
            .WithMany(entity => entity.Subscriptions)
            .HasForeignKey(entity => entity.CompanyId);

        builder.Entity<AppUserRole>()
            .HasIndex(entity => new { entity.AppUserId, entity.CompanyId, entity.RoleName })
            .IsUnique();
        builder.Entity<AppUserRole>()
            .HasOne(entity => entity.Company)
            .WithMany(entity => entity.UserRoles)
            .HasForeignKey(entity => entity.CompanyId);
        builder.Entity<AppUserRole>()
            .HasOne(entity => entity.AppUser)
            .WithMany(entity => entity.CompanyRoles)
            .HasForeignKey(entity => entity.AppUserId);

        builder.Entity<ToothRecord>().HasIndex(entity => new { entity.CompanyId, entity.PatientId, entity.ToothNumber }).IsUnique();
        builder.Entity<PlanItem>().HasIndex(entity => new { entity.CompanyId, entity.TreatmentPlanId, entity.Sequence }).IsUnique();
        builder.Entity<TreatmentRoom>().HasIndex(entity => new { entity.CompanyId, entity.Code }).IsUnique();
        builder.Entity<Invoice>().HasIndex(entity => new { entity.CompanyId, entity.InvoiceNumber }).IsUnique();

        builder.Entity<TreatmentType>().Property(entity => entity.BasePrice).HasPrecision(12, 2);
        builder.Entity<Treatment>().Property(entity => entity.Price).HasPrecision(12, 2);
        builder.Entity<PlanItem>().Property(entity => entity.EstimatedPrice).HasPrecision(12, 2);
        builder.Entity<CostEstimate>().Property(entity => entity.TotalEstimatedAmount).HasPrecision(12, 2);
        builder.Entity<Invoice>().Property(entity => entity.TotalAmount).HasPrecision(12, 2);
        builder.Entity<Invoice>().Property(entity => entity.BalanceAmount).HasPrecision(12, 2);
        builder.Entity<PaymentPlan>().Property(entity => entity.InstallmentAmount).HasPrecision(12, 2);

        ConfigureTenantFilter<CompanySettings>(builder);
        ConfigureTenantFilter<Subscription>(builder);
        ConfigureTenantFilter<AppUserRole>(builder);

        ConfigureTenantSoftDeleteFilter<Patient>(builder);
        ConfigureTenantSoftDeleteFilter<ToothRecord>(builder);
        ConfigureTenantSoftDeleteFilter<TreatmentType>(builder);
        ConfigureTenantSoftDeleteFilter<Treatment>(builder);
        ConfigureTenantSoftDeleteFilter<Appointment>(builder);
        ConfigureTenantSoftDeleteFilter<TreatmentPlan>(builder);
        ConfigureTenantSoftDeleteFilter<PlanItem>(builder);
        ConfigureTenantSoftDeleteFilter<Xray>(builder);
        ConfigureTenantSoftDeleteFilter<InsurancePlan>(builder);
        ConfigureTenantSoftDeleteFilter<CostEstimate>(builder);
        ConfigureTenantSoftDeleteFilter<Invoice>(builder);
        ConfigureTenantSoftDeleteFilter<PaymentPlan>(builder);
        ConfigureTenantSoftDeleteFilter<Dentist>(builder);
        ConfigureTenantSoftDeleteFilter<TreatmentRoom>(builder);
    }

    private void ConfigureTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(entity =>
            _tenantProvider.IgnoreTenantFilter || entity.CompanyId == _tenantProvider.CompanyId);
    }

    private void ConfigureTenantSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity, ISoftDeleteEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(entity =>
            (_tenantProvider.IgnoreTenantFilter || entity.CompanyId == _tenantProvider.CompanyId) && !entity.IsDeleted);
    }

    private void ApplyAuditFields()
    {
        var nowUtc = DateTime.UtcNow;
        var userId = GetCurrentUserId();

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = nowUtc;
                entry.Entity.CreatedByUserId ??= userId;
            }

            if (entry.State is EntityState.Modified or EntityState.Added)
            {
                entry.Entity.ModifiedAtUtc = nowUtc;
                entry.Entity.ModifiedByUserId = userId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(entry => entry.State == EntityState.Added))
        {
            if (entry.Entity.CompanyId == Guid.Empty && _tenantProvider.CompanyId.HasValue)
            {
                entry.Entity.CompanyId = _tenantProvider.CompanyId.Value;
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
        var userId = GetCurrentUserId();
        var nowUtc = DateTime.UtcNow;

        var entries = ChangeTracker
            .Entries()
            .Where(entry =>
                entry.Entity is not AuditLog &&
                entry.Entity is ITenantEntity &&
                entry.Entity is IBaseEntity &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var logs = new List<AuditLog>(entries.Count);

        foreach (var entry in entries)
        {
            var tenantEntity = (ITenantEntity)entry.Entity;
            var baseEntity = (IBaseEntity)entry.Entity;

            logs.Add(new AuditLog
            {
                CompanyId = tenantEntity.CompanyId,
                ActorUserId = userId,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = baseEntity.Id,
                Action = entry.State.ToString(),
                ChangedAtUtc = nowUtc,
                ChangesJson = SerializeChangeSet(entry)
            });
        }

        return logs;
    }

    private static string SerializeChangeSet(EntityEntry entry)
    {
        var dictionary = entry.Properties.ToDictionary(
            property => property.Metadata.Name,
            property => new
            {
                OldValue = entry.State == EntityState.Added ? null : property.OriginalValue,
                NewValue = entry.State == EntityState.Deleted ? null : property.CurrentValue
            });

        return JsonSerializer.Serialize(dictionary);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
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
}
