using System.Globalization;
using App.BLL.Services.Admin;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using App.DAL.EF.Tenant;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.Services;

namespace WebApp.Tests.Unit;

public class AdminOperationsPageServiceTests
{
    [Fact]
    public async Task BuildAsync_ProjectsSnapshotIntoViewModel()
    {
        var gymId = Guid.NewGuid();
        var snapshot = new AdminOperationsSnapshot(
            OpeningHours: new[]
            {
                new AdminOpeningHoursRow(1, new TimeOnly(8, 0), new TimeOnly(20, 0))
            },
            Equipment: new[]
            {
                new AdminEquipmentRow("EQ-100", new LangStr("Treadmill", "en"), EquipmentStatus.Active)
            },
            MaintenanceTasks: new[]
            {
                new AdminMaintenanceTaskRow(
                    "EQ-100",
                    MaintenanceTaskType.Scheduled,
                    MaintenanceTaskStatus.Open,
                    "Care Taker",
                    new DateTime(2026, 06, 01, 12, 0, 0, DateTimeKind.Utc))
            });

        var stub = new StubAdminOperationsQueryService(snapshot);
        var service = new AdminOperationsPageService(stub);

        var viewModel = await service.BuildAsync(gymId, "demo-gym");

        Assert.Equal(gymId, stub.LastGymId);
        Assert.Equal("demo-gym", viewModel.GymCode);

        var hours = Assert.Single(viewModel.OpeningHours);
        Assert.Equal(1, hours.Weekday);
        Assert.Equal(new TimeOnly(8, 0), hours.OpensAt);

        var equipment = Assert.Single(viewModel.Equipment);
        Assert.Equal("EQ-100", equipment.AssetTag);
        Assert.Equal("Treadmill", equipment.ModelName);
        Assert.Equal(EquipmentStatus.Active, equipment.Status);

        var task = Assert.Single(viewModel.MaintenanceTasks);
        Assert.Equal("EQ-100", task.AssetTag);
        Assert.Equal(MaintenanceTaskType.Scheduled, task.TaskType);
        Assert.Equal("Care Taker", task.AssignedTo);
    }

    [Fact]
    public async Task BuildAsync_TranslatesEquipmentModelNameToActiveCulture()
    {
        var snapshot = new AdminOperationsSnapshot(
            OpeningHours: Array.Empty<AdminOpeningHoursRow>(),
            Equipment: new[]
            {
                new AdminEquipmentRow(
                    "EQ-200",
                    new LangStr { ["en"] = "Treadmill", ["et"] = "Jooksulint" },
                    EquipmentStatus.Active)
            },
            MaintenanceTasks: Array.Empty<AdminMaintenanceTaskRow>());

        var service = new AdminOperationsPageService(new StubAdminOperationsQueryService(snapshot));
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("et-EE");
            var viewModel = await service.BuildAsync(Guid.NewGuid(), "g");

            Assert.Equal("Jooksulint", Assert.Single(viewModel.Equipment).ModelName);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public async Task BuildAsync_NullModelNameProjectsToEmptyString()
    {
        var snapshot = new AdminOperationsSnapshot(
            OpeningHours: Array.Empty<AdminOpeningHoursRow>(),
            Equipment: new[]
            {
                new AdminEquipmentRow("EQ-300", null, EquipmentStatus.Active)
            },
            MaintenanceTasks: Array.Empty<AdminMaintenanceTaskRow>());

        var service = new AdminOperationsPageService(new StubAdminOperationsQueryService(snapshot));

        var viewModel = await service.BuildAsync(Guid.NewGuid(), "g");

        Assert.Equal(string.Empty, Assert.Single(viewModel.Equipment).ModelName);
    }

    [Fact]
    public async Task QueryService_ReturnsTopEquipmentAndIncompleteTasksOrderedAsExpected()
    {
        await using var dbContext = CreateDbContext();
        var gymId = await SeedOperationsFixtureAsync(dbContext);
        var queryService = new AdminOperationsQueryService(new EfAppUnitOfWork(dbContext));

        var snapshot = await queryService.GetSnapshotAsync(gymId);

        Assert.Equal(2, snapshot.OpeningHours.Count);
        Assert.Equal(1, snapshot.OpeningHours[0].Weekday);
        Assert.Equal(2, snapshot.OpeningHours[1].Weekday);

        Assert.Equal(2, snapshot.Equipment.Count);
        Assert.All(snapshot.Equipment, row => Assert.NotNull(row.ModelName));

        Assert.Equal(2, snapshot.MaintenanceTasks.Count);
        Assert.All(snapshot.MaintenanceTasks, row =>
            Assert.NotEqual(MaintenanceTaskStatus.Done, row.Status));
        Assert.Equal("Care Taker", snapshot.MaintenanceTasks[0].AssignedTo);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AdminOperations-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options, new TestGymContext(), new HttpContextAccessor());
    }

    private static async Task<Guid> SeedOperationsFixtureAsync(AppDbContext dbContext)
    {
        var gym = new Gym
        {
            Name = "Ops Gym",
            Code = "ops-gym",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        };
        dbContext.Gyms.Add(gym);

        dbContext.OpeningHours.AddRange(
            new OpeningHours { GymId = gym.Id, Weekday = 2, OpensAt = new TimeOnly(8, 0), ClosesAt = new TimeOnly(20, 0) },
            new OpeningHours { GymId = gym.Id, Weekday = 1, OpensAt = new TimeOnly(7, 0), ClosesAt = new TimeOnly(21, 0) });

        var model = new EquipmentModel
        {
            GymId = gym.Id,
            Name = new LangStr("Treadmill", "en"),
            Type = EquipmentType.Cardio
        };
        var equipmentA = new Equipment
        {
            GymId = gym.Id,
            EquipmentModel = model,
            AssetTag = "EQ-001",
            CurrentStatus = EquipmentStatus.Active
        };
        var equipmentB = new Equipment
        {
            GymId = gym.Id,
            EquipmentModel = model,
            AssetTag = "EQ-002",
            CurrentStatus = EquipmentStatus.Maintenance
        };
        dbContext.EquipmentModels.Add(model);
        dbContext.Equipment.AddRange(equipmentA, equipmentB);

        var caretakerPerson = new Person { FirstName = "Care", LastName = "Taker" };
        var staff = new Staff
        {
            GymId = gym.Id,
            Person = caretakerPerson,
            StaffCode = "STF-CARE",
            Status = StaffStatus.Active
        };
        dbContext.People.Add(caretakerPerson);
        dbContext.Staff.Add(staff);

        var openTask = new MaintenanceTask
        {
            GymId = gym.Id,
            Equipment = equipmentA,
            AssignedStaff = staff,
            TaskType = MaintenanceTaskType.Scheduled,
            Status = MaintenanceTaskStatus.Open,
            DueAtUtc = new DateTime(2026, 06, 01, 12, 0, 0, DateTimeKind.Utc)
        };
        var inProgressTask = new MaintenanceTask
        {
            GymId = gym.Id,
            Equipment = equipmentB,
            TaskType = MaintenanceTaskType.Breakdown,
            Status = MaintenanceTaskStatus.InProgress,
            DueAtUtc = new DateTime(2026, 06, 15, 12, 0, 0, DateTimeKind.Utc)
        };
        var doneTask = new MaintenanceTask
        {
            GymId = gym.Id,
            Equipment = equipmentA,
            TaskType = MaintenanceTaskType.Scheduled,
            Status = MaintenanceTaskStatus.Done,
            DueAtUtc = new DateTime(2026, 05, 01, 12, 0, 0, DateTimeKind.Utc)
        };
        dbContext.MaintenanceTasks.AddRange(openTask, inProgressTask, doneTask);

        await dbContext.SaveChangesAsync();
        return gym.Id;
    }

    private sealed class StubAdminOperationsQueryService(AdminOperationsSnapshot snapshot) : IAdminOperationsQueryService
    {
        public Guid LastGymId { get; private set; }

        public Task<AdminOperationsSnapshot> GetSnapshotAsync(Guid gymId, CancellationToken cancellationToken = default)
        {
            LastGymId = gymId;
            return Task.FromResult(snapshot);
        }
    }

    private sealed class TestGymContext : IGymContext
    {
        public Guid? GymId => null;
        public string? GymCode => null;
        public string? ActiveRole => null;
        public bool IgnoreGymFilter => true;
    }
}
