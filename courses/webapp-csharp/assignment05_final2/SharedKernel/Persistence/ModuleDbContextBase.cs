using System.Text.Json;
using Base.Contracts;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharedKernel.Common;

namespace SharedKernel.Persistence;

/// <summary>
/// Shared EF behavior for module-owned DbContexts.
/// </summary>
public abstract class ModuleDbContextBase<TContext>(
    DbContextOptions<TContext> options,
    IGymContext gymContext)
    : DbContext(options)
    where TContext : DbContext
{
    private readonly IGymContext _gymContext = gymContext;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        ApplySoftDelete();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<LangStr>()
            .HaveConversion<LangStrValueConverter, LangStrValueComparer>();
    }

    protected void ConfigureModuleDefaults(ModelBuilder builder)
    {
        builder.Ignore<LangStr>();
        ConfigureDateTimeAsUtc(builder);
        ConfigureLangStrAsJsonb(builder);

        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    protected void ConfigureTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(entity =>
            _gymContext.IgnoreGymFilter || entity.GymId == _gymContext.GymId);
    }

    protected void ConfigureTenantSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity, ISoftDeleteEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(entity =>
            (_gymContext.IgnoreGymFilter || entity.GymId == _gymContext.GymId) && !entity.IsDeleted);
    }

    private sealed class LangStrValueConverter()
        : ValueConverter<LangStr, string>(
            value => JsonSerializer.Serialize(value ?? new LangStr(), JsonSerializerOptions.Web),
            value => string.IsNullOrWhiteSpace(value)
                ? new LangStr()
                : JsonSerializer.Deserialize<LangStr>(value, JsonSerializerOptions.Web) ?? new LangStr());

    private sealed class LangStrValueComparer()
        : ValueComparer<LangStr>(
            (left, right) => JsonSerializer.Serialize(left ?? new LangStr(), JsonSerializerOptions.Web) ==
                             JsonSerializer.Serialize(right ?? new LangStr(), JsonSerializerOptions.Web),
            value => JsonSerializer.Serialize(value ?? new LangStr(), JsonSerializerOptions.Web).GetHashCode(),
            value => JsonSerializer.Deserialize<LangStr>(
                         JsonSerializer.Serialize(value ?? new LangStr(), JsonSerializerOptions.Web),
                         JsonSerializerOptions.Web)
                     ?? new LangStr());

    private void ApplyAuditFields()
    {
        var nowUtc = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = nowUtc;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = nowUtc;
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
