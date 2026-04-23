using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public sealed class OpeningHoursConfiguration : IEntityTypeConfiguration<OpeningHours>
{
    public void Configure(EntityTypeBuilder<OpeningHours> builder)
    {
        builder.HasIndex(hours => new { hours.GymId, hours.Weekday, hours.ValidFrom, hours.ValidTo });
    }
}

public sealed class OpeningHoursExceptionConfiguration : IEntityTypeConfiguration<OpeningHoursException>
{
    public void Configure(EntityTypeBuilder<OpeningHoursException> builder)
    {
        builder.HasIndex(exceptionEntity => new { exceptionEntity.GymId, exceptionEntity.ExceptionDate })
            .IsUnique();
    }
}

public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.HasOne(equipment => equipment.EquipmentModel)
            .WithMany(model => model.EquipmentItems)
            .HasForeignKey(equipment => equipment.EquipmentModelId);

        builder.HasIndex(equipment => new { equipment.GymId, equipment.AssetTag })
            .IsUnique();

        builder.HasIndex(equipment => new { equipment.GymId, equipment.SerialNumber })
            .IsUnique();
    }
}

public sealed class MaintenanceTaskConfiguration : IEntityTypeConfiguration<MaintenanceTask>
{
    public void Configure(EntityTypeBuilder<MaintenanceTask> builder)
    {
        builder.HasOne(task => task.Equipment)
            .WithMany(equipment => equipment.MaintenanceTasks)
            .HasForeignKey(task => task.EquipmentId);

        builder.HasOne(task => task.AssignedStaff)
            .WithMany(staff => staff.AssignedTasks)
            .HasForeignKey(task => task.AssignedStaffId);

        builder.HasOne(task => task.CreatedByStaff)
            .WithMany(staff => staff.CreatedTasks)
            .HasForeignKey(task => task.CreatedByStaffId);

        builder.HasIndex(task => new { task.GymId, task.EquipmentId, task.Status, task.DueAtUtc });
    }
}

public sealed class MaintenanceTaskAssignmentHistoryConfiguration : IEntityTypeConfiguration<MaintenanceTaskAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<MaintenanceTaskAssignmentHistory> builder)
    {
        builder.HasOne(history => history.MaintenanceTask)
            .WithMany(task => task.AssignmentHistory)
            .HasForeignKey(history => history.MaintenanceTaskId);

        builder.HasOne(history => history.AssignedStaff)
            .WithMany(staff => staff.MaintenanceAssignmentEvents)
            .HasForeignKey(history => history.AssignedStaffId);

        builder.HasOne(history => history.AssignedByStaff)
            .WithMany(staff => staff.MaintenanceAssignmentChanges)
            .HasForeignKey(history => history.AssignedByStaffId);

        builder.HasIndex(history => new { history.GymId, history.MaintenanceTaskId, history.AssignedAtUtc });
    }
}
