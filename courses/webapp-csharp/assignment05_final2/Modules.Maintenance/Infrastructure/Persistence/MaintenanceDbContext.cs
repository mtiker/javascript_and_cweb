using SharedKernel.Persistence;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Modules.Maintenance.Infrastructure.Persistence;

public sealed class MaintenanceDbContext(
    DbContextOptions<MaintenanceDbContext> options,
    IGymContext gymContext)
    : ModuleDbContextBase<MaintenanceDbContext>(options, gymContext)
{
    public DbSet<EquipmentModel> EquipmentModels { get; set; } = default!;
    public DbSet<Equipment> Equipment { get; set; } = default!;
    public DbSet<MaintenanceTask> MaintenanceTasks { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("maintenance");

        builder.Entity<Equipment>(entity =>
        {
            entity.HasOne(equipment => equipment.EquipmentModel)
                .WithMany(model => model.EquipmentItems)
                .HasForeignKey(equipment => equipment.EquipmentModelId);

            entity.HasIndex(equipment => new { equipment.GymId, equipment.AssetTag })
                .IsUnique();

            entity.HasIndex(equipment => new { equipment.GymId, equipment.SerialNumber })
                .IsUnique();
        });

        builder.Entity<MaintenanceTask>(entity =>
        {
            entity.HasOne(task => task.Equipment)
                .WithMany(equipment => equipment.MaintenanceTasks)
                .HasForeignKey(task => task.EquipmentId);

            entity.Ignore(task => task.AssignedStaff);
            entity.Ignore(task => task.CreatedByStaff);

            entity.HasIndex(task => new { task.GymId, task.EquipmentId, task.Status, task.DueAtUtc });
        });

        ConfigureTenantSoftDeleteFilter<EquipmentModel>(builder);
        ConfigureTenantSoftDeleteFilter<Equipment>(builder);
        ConfigureTenantSoftDeleteFilter<MaintenanceTask>(builder);
        ConfigureModuleDefaults(builder);
    }
}
