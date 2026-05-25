using SharedKernel.Exceptions;
using App.BLL.Contracts.Services;
using Modules.Gyms.Application.Authorization;
using Modules.Gyms.Application.Platform;
using Modules.Memberships.Application;
using WebApp.Areas.Admin.Queries;
using WebApp.Areas.Client.Queries;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using Modules.Gyms.Infrastructure;
using Modules.Users.Application;
using SharedKernel.Persistence;
using Modules.Maintenance.Application;
using Modules.Maintenance.Application.Mappers;
using Modules.Maintenance.Infrastructure;
using Modules.Training.Infrastructure;
using Modules.Users.Infrastructure;
using Shared.Contracts.ModuleApis;
using SharedKernel;
using Base.Domain;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.MaintenanceTasks;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Unit;

public class MaintenanceWorkflowServiceTests
{
    private const string GymCode = "maintenance-test-gym";

    [Fact]
    public async Task UpdateTaskStatusAsync_AllowsAssignedCaretaker()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedMaintenanceFixtureAsync(dbContext);
        var service = CreateService(dbContext, seed.GymId, seed.CaretakerPersonId, RoleNames.Caretaker);

        var response = await service.UpdateTaskStatusAsync(GymCode, seed.AssignedTaskId, new MaintenanceStatusUpdateRequest
        {
            Status = MaintenanceTaskStatus.InProgress,
            Notes = "  Started inspection  "
        });

        Assert.Equal(MaintenanceTaskStatus.InProgress, response.Status);
        Assert.Equal("Started inspection", response.Notes);

        var stored = await dbContext.MaintenanceTasks.SingleAsync(task => task.Id == seed.AssignedTaskId);
        Assert.Equal(MaintenanceTaskStatus.InProgress, stored.Status);
        Assert.NotNull(stored.StartedAtUtc);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_RejectsUnassignedCaretaker()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedMaintenanceFixtureAsync(dbContext);
        var service = CreateService(dbContext, seed.GymId, seed.CaretakerPersonId, RoleNames.Caretaker);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateTaskStatusAsync(GymCode, seed.UnassignedTaskId, new MaintenanceStatusUpdateRequest
            {
                Status = MaintenanceTaskStatus.InProgress
            }));

        var stored = await dbContext.MaintenanceTasks.SingleAsync(task => task.Id == seed.UnassignedTaskId);
        Assert.Equal(MaintenanceTaskStatus.Open, stored.Status);
    }

    [Fact]
    public async Task GenerateDueScheduledTasksAsync_CreatesOneOpenScheduledTaskPerDueEquipment()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedMaintenanceFixtureAsync(dbContext, includeOpenTasks: false);
        var service = CreateService(dbContext, seed.GymId, seed.AdminPersonId, RoleNames.GymAdmin);

        var created = await service.GenerateDueScheduledTasksAsync(GymCode);
        var repeated = await service.GenerateDueScheduledTasksAsync(GymCode);

        Assert.Equal(1, created);
        Assert.Equal(0, repeated);

        var generated = await dbContext.MaintenanceTasks.SingleAsync(task =>
            task.GymId == seed.GymId &&
            task.EquipmentId == seed.EquipmentId &&
            task.TaskType == MaintenanceTaskType.Scheduled);
        Assert.Equal(MaintenanceTaskStatus.Open, generated.Status);
        Assert.NotNull(generated.DueAtUtc);
    }

    [Fact]
    public async Task UpdateTaskAssignmentAsync_UpdatesAssignedStaff()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedMaintenanceFixtureAsync(dbContext);
        var service = CreateService(dbContext, seed.GymId, seed.AdminPersonId, RoleNames.GymAdmin);

        var response = await service.UpdateTaskAssignmentAsync(GymCode, seed.UnassignedTaskId, new MaintenanceAssignmentUpdateRequest
        {
            AssignedStaffId = seed.CaretakerStaffId,
            AssignedByStaffId = seed.AdminStaffId,
            Notes = "  Escalated to caretaker  "
        });

        Assert.Equal(seed.CaretakerStaffId, response.AssignedStaffId);
        Assert.Equal("Escalated to caretaker", response.Notes);
    }

    [Fact]
    public async Task BreakdownStatusUpdates_MoveEquipmentIntoAndOutOfMaintenance()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedMaintenanceFixtureAsync(dbContext, includeOpenTasks: false);
        var breakdown = new MaintenanceTask
        {
            GymId = seed.GymId,
            EquipmentId = seed.EquipmentId,
            AssignedStaffId = seed.CaretakerStaffId,
            TaskType = MaintenanceTaskType.Breakdown,
            Priority = MaintenancePriority.High,
            Status = MaintenanceTaskStatus.Open
        };
        dbContext.MaintenanceTasks.Add(breakdown);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, seed.GymId, seed.CaretakerPersonId, RoleNames.Caretaker);

        var inProgress = await service.UpdateTaskStatusAsync(GymCode, breakdown.Id, new MaintenanceStatusUpdateRequest
        {
            Status = MaintenanceTaskStatus.InProgress
        });
        var completed = await service.UpdateTaskStatusAsync(GymCode, breakdown.Id, new MaintenanceStatusUpdateRequest
        {
            Status = MaintenanceTaskStatus.Done,
            CompletionNotes = "  Belt replaced  "
        });

        Assert.NotNull(inProgress.DowntimeStartedAtUtc);
        Assert.NotNull(completed.DowntimeEndedAtUtc);
        Assert.Equal("Belt replaced", completed.CompletionNotes);

        var equipment = await dbContext.Equipment.SingleAsync(item => item.Id == seed.EquipmentId);
        Assert.Equal(EquipmentStatus.Active, equipment.CurrentStatus);
    }

    private static IMaintenanceWorkflowService CreateService(
        AppDbContext dbContext,
        Guid gymId,
        Guid personId,
        string activeRole)
    {
        return new MaintenanceWorkflowService(
            new EfMaintenancePersistenceContext(dbContext),
            new EfMaintenanceRepository(dbContext),
            CreateAuthorizationService(dbContext, gymId, personId, activeRole),
            new NoopSubscriptionTierLimitService(),
            new TestTrainingModuleApi(dbContext),
            new MaintenanceMapper());
    }

    private static IAuthorizationService CreateAuthorizationService(
        AppDbContext dbContext,
        Guid gymId,
        Guid personId,
        string activeRole)
    {
        var context = new UserExecutionContext(
            UserId: Guid.NewGuid(),
            PersonId: personId,
            ActiveGymId: gymId,
            ActiveGymCode: GymCode,
            ActiveRole: activeRole,
            AllRoles: [activeRole],
            SystemRoles: []);
        var userContextService = new TestUserContextService(context);
        var currentActorResolver = new CurrentActorResolver(dbContext, userContextService);

        return new AuthorizationService(
            currentActorResolver,
            new TenantAccessChecker(new EfAuthorizationQueryRepository(dbContext), currentActorResolver),
            new ResourceAuthorizationChecker(dbContext, currentActorResolver));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"MaintenanceWorkflowService-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options, new TestGymContext());
    }

    private static async Task<MaintenanceFixture> SeedMaintenanceFixtureAsync(
        AppDbContext dbContext,
        bool includeOpenTasks = true)
    {
        var gym = new Gym
        {
            Name = "Maintenance Test Gym",
            Code = GymCode,
            AddressLine = "Demo street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        };
        var caretakerPerson = CreatePerson("Care", "Taker");
        var otherPerson = CreatePerson("Other", "Staff");
        var adminPerson = CreatePerson("Admin", "Person");
        var caretakerStaff = CreateStaff(gym.Id, caretakerPerson, "STF-CARE");
        var otherStaff = CreateStaff(gym.Id, otherPerson, "STF-OTHER");
        var adminStaff = CreateStaff(gym.Id, adminPerson, "STF-ADMIN");
        var model = new EquipmentModel
        {
            GymId = gym.Id,
            Name = new LangStr("Treadmill", "en"),
            Type = EquipmentType.Cardio,
            MaintenanceIntervalDays = 30
        };
        var equipment = new Equipment
        {
            GymId = gym.Id,
            EquipmentModel = model,
            AssetTag = "EQ-100",
            CurrentStatus = EquipmentStatus.Active,
            CommissionedAt = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-60))
        };

        dbContext.Gyms.Add(gym);
        dbContext.People.AddRange(caretakerPerson, otherPerson, adminPerson);
        dbContext.Staff.AddRange(caretakerStaff, otherStaff, adminStaff);
        dbContext.EquipmentModels.Add(model);
        dbContext.Equipment.Add(equipment);

        var assignedTask = new MaintenanceTask
        {
            GymId = gym.Id,
            Equipment = equipment,
            AssignedStaff = caretakerStaff,
            TaskType = MaintenanceTaskType.Scheduled,
            Status = MaintenanceTaskStatus.Open
        };
        var unassignedTask = new MaintenanceTask
        {
            GymId = gym.Id,
            Equipment = equipment,
            AssignedStaff = otherStaff,
            TaskType = MaintenanceTaskType.Breakdown,
            Status = MaintenanceTaskStatus.Open
        };

        if (includeOpenTasks)
        {
            dbContext.MaintenanceTasks.AddRange(assignedTask, unassignedTask);
        }

        await dbContext.SaveChangesAsync();

        return new MaintenanceFixture(
            gym.Id,
            equipment.Id,
            caretakerPerson.Id,
            caretakerStaff.Id,
            adminPerson.Id,
            adminStaff.Id,
            includeOpenTasks ? assignedTask.Id : Guid.Empty,
            includeOpenTasks ? unassignedTask.Id : Guid.Empty);
    }

    private static Person CreatePerson(string firstName, string lastName) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            PersonalCode = $"{Guid.NewGuid():N}"[..11]
        };

    private static Staff CreateStaff(Guid gymId, Person person, string code) =>
        new()
        {
            GymId = gymId,
            Person = person,
            PersonId = person.Id,
            StaffCode = code,
            Status = StaffStatus.Active
        };

    private sealed record MaintenanceFixture(
        Guid GymId,
        Guid EquipmentId,
        Guid CaretakerPersonId,
        Guid CaretakerStaffId,
        Guid AdminPersonId,
        Guid AdminStaffId,
        Guid AssignedTaskId,
        Guid UnassignedTaskId);

    private sealed class TestUserContextService(UserExecutionContext context) : IUserContextService
    {
        public UserExecutionContext GetCurrent() => context;
    }

    private sealed class TestGymContext : IGymContext
    {
        public Guid? GymId => null;
        public string? GymCode => null;
        public string? ActiveRole => null;
        public bool IgnoreGymFilter => true;
    }

    private sealed class NoopSubscriptionTierLimitService : ISubscriptionTierLimitService
    {
        public Task EnsureCanCreateMemberAsync(Guid gymId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task EnsureCanCreateStaffAsync(Guid gymId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task EnsureCanCreateTrainingSessionAsync(Guid gymId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task EnsureCanCreateEquipmentAsync(Guid gymId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class TestTrainingModuleApi(AppDbContext dbContext) : ITrainingModuleApi
    {
        public async Task<StaffSummary?> GetStaffSummaryAsync(
            Guid gymId,
            Guid staffId,
            CancellationToken cancellationToken = default)
        {
            var staff = await dbContext.Staff
                .Include(entity => entity.Person)
                .FirstOrDefaultAsync(entity => entity.GymId == gymId && entity.Id == staffId, cancellationToken);

            return staff is null
                ? null
                : new StaffSummary(
                    staff.Id,
                    staff.GymId,
                    staff.StaffCode,
                    $"{staff.Person?.FirstName} {staff.Person?.LastName}".Trim(),
                    staff.Status.ToString());
        }

        public Task<TrainingSessionSummary?> GetTrainingSessionSummaryAsync(
            Guid gymId,
            Guid sessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TrainingSessionSummary?>(null);

        public Task<BookingSummary?> GetBookingSummaryAsync(
            Guid gymId,
            Guid bookingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BookingSummary?>(null);

        public Task<IReadOnlyList<Guid>> ListBookingIdsForMemberAsync(
            Guid gymId,
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    }
}
