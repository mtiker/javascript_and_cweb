using App.BLL.Contracts.Services;
using App.BLL.Services;
using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using Base.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.MaintenanceTasks;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Client.Services;

namespace WebApp.Tests.Unit;

public class Final1PresentationServiceTests
{
    [Fact]
    public async Task WorkspaceContext_ReturnsTenantUserGymsAndActiveRoles()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var gym = CreateGym("peak-forge", "Peak Forge", isActive: true);
        dbContext.Gyms.Add(gym);
        dbContext.AppUserGymRoles.AddRange(
            new AppUserGymRole { AppUserId = userId, GymId = gym.Id, Gym = gym, RoleName = RoleNames.GymAdmin, IsActive = true },
            new AppUserGymRole { AppUserId = userId, GymId = gym.Id, Gym = gym, RoleName = RoleNames.Trainer, IsActive = true });
        await dbContext.SaveChangesAsync();

        var service = new WorkspaceContextService(dbContext);
        var options = await service.GetSwitchOptionsAsync(userId, isSystemAdmin: false, activeGymCode: "peak-forge");

        var gymOption = Assert.Single(options.Gyms);
        Assert.Equal("peak-forge", gymOption.Code);
        Assert.Equal("Peak Forge", gymOption.Name);
        Assert.Contains(RoleNames.GymAdmin, options.RolesInActiveGym);
        Assert.Contains(RoleNames.Trainer, options.RolesInActiveGym);
    }

    [Fact]
    public async Task WorkspaceContext_SystemAdminCanSelectAnyActiveGym()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Gyms.AddRange(
            CreateGym("peak-forge", "Peak Forge", isActive: true),
            CreateGym("closed-gym", "Closed Gym", isActive: false));
        await dbContext.SaveChangesAsync();

        var service = new WorkspaceContextService(dbContext);
        var options = await service.GetSwitchOptionsAsync(userId, isSystemAdmin: true, activeGymCode: "peak-forge");
        var syntheticLink = await service.BuildSystemAdminGymRoleAsync(userId, "peak-forge", RoleNames.GymOwner);

        var gymOption = Assert.Single(options.Gyms);
        Assert.Equal("peak-forge", gymOption.Code);
        Assert.Contains(RoleNames.GymOwner, options.RolesInActiveGym);
        Assert.Contains(RoleNames.GymAdmin, options.RolesInActiveGym);
        Assert.NotNull(syntheticLink);
        Assert.Equal(RoleNames.GymOwner, syntheticLink!.RoleName);
    }

    [Fact]
    public async Task ClientProfilePageService_BuildsCurrentMemberProfile()
    {
        await using var dbContext = CreateDbContext();
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Marta", LastName = "Member" };
        var member = new Member { Id = memberId, GymId = gymId, PersonId = person.Id, Person = person, MemberCode = "MEM-900", Status = MemberStatus.Active };
        var package = new MembershipPackage
        {
            Id = packageId,
            GymId = gymId,
            Name = new LangStr("Gold", "en"),
            PackageType = MembershipPackageType.Monthly,
            DurationValue = 1,
            DurationUnit = DurationUnit.Month,
            BasePrice = 50,
            CurrencyCode = "EUR"
        };
        var membership = new Membership
        {
            Id = membershipId,
            GymId = gymId,
            MemberId = memberId,
            Member = member,
            MembershipPackageId = packageId,
            MembershipPackage = package,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 2, 1),
            PriceAtPurchase = 50,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Active
        };
        var session = new TrainingSession
        {
            Id = sessionId,
            GymId = gymId,
            Name = new LangStr("Strength", "en"),
            StartAtUtc = DateTime.UtcNow.AddDays(1),
            EndAtUtc = DateTime.UtcNow.AddDays(1).AddHours(1),
            Capacity = 12,
            CurrencyCode = "EUR"
        };
        var booking = new Booking
        {
            Id = bookingId,
            GymId = gymId,
            MemberId = memberId,
            Member = member,
            TrainingSessionId = sessionId,
            TrainingSession = session,
            Status = BookingStatus.Booked,
            ChargedPrice = 10,
            CurrencyCode = "EUR",
            BookedAtUtc = DateTime.UtcNow
        };
        dbContext.People.Add(person);
        dbContext.Members.Add(member);
        dbContext.MembershipPackages.Add(package);
        dbContext.Memberships.Add(membership);
        dbContext.TrainingSessions.Add(session);
        dbContext.Bookings.Add(booking);
        dbContext.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            MembershipId = membershipId,
            Membership = membership,
            Amount = 50,
            CurrencyCode = "EUR",
            Status = PaymentStatus.Completed,
            PaidAtUtc = DateTime.UtcNow,
            Reference = "paid"
        });
        await dbContext.SaveChangesAsync();

        var service = new ClientProfilePageService(
            dbContext,
            new StubUserContextService(gymId, "peak-forge", RoleNames.Member),
            new StubAuthorizationService(currentMember: member));

        var model = await service.BuildAsync();

        Assert.NotNull(model);
        Assert.True(model!.ProfileAvailable);
        Assert.Equal("Marta Member", model.MemberName);
        Assert.Equal("MEM-900", model.MemberCode);
        Assert.Single(model.Memberships);
        Assert.Single(model.Bookings);
        Assert.Single(model.Payments);
    }

    [Fact]
    public async Task ClientProfilePageService_ReturnsUnavailableModelWhenUserHasNoMemberProfile()
    {
        await using var dbContext = CreateDbContext();
        var gymId = Guid.NewGuid();
        var service = new ClientProfilePageService(
            dbContext,
            new StubUserContextService(gymId, "peak-forge", RoleNames.Member),
            new StubAuthorizationService());

        var model = await service.BuildAsync();

        Assert.NotNull(model);
        Assert.False(model!.ProfileAvailable);
        Assert.Equal("peak-forge", model.GymCode);
    }

    [Fact]
    public async Task ClientMaintenancePageService_ReturnsAssignedTasksAndEquipmentLabel()
    {
        await using var dbContext = CreateDbContext();
        var gymId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var equipmentId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        dbContext.Equipment.Add(new Equipment
        {
            Id = equipmentId,
            GymId = gymId,
            AssetTag = "TREAD-01"
        });
        await dbContext.SaveChangesAsync();

        var maintenance = new StubMaintenanceWorkflowService([
            new MaintenanceTaskResponse
            {
                Id = taskId,
                EquipmentId = equipmentId,
                AssignedStaffId = staffId,
                EquipmentName = "Treadmill",
                Status = MaintenanceTaskStatus.Open,
                TaskType = MaintenanceTaskType.Scheduled,
                Priority = MaintenancePriority.Medium
            },
            new MaintenanceTaskResponse
            {
                Id = Guid.NewGuid(),
                EquipmentId = Guid.NewGuid(),
                AssignedStaffId = Guid.NewGuid(),
                EquipmentName = "Bike",
                Status = MaintenanceTaskStatus.Open,
                TaskType = MaintenanceTaskType.Scheduled,
                Priority = MaintenancePriority.Low
            }
        ]);
        var service = new ClientMaintenancePageService(
            dbContext,
            new StubUserContextService(gymId, "peak-forge", RoleNames.Caretaker),
            new StubAuthorizationService(currentStaff: new Staff { Id = staffId, GymId = gymId }),
            maintenance);

        var index = await service.BuildIndexAsync();
        var detail = await service.BuildDetailsAsync(taskId);
        var updated = await service.UpdateStatusAsync(taskId, MaintenanceTaskStatus.InProgress, "Started");

        Assert.NotNull(index);
        var task = Assert.Single(index!.Tasks);
        Assert.Equal(taskId, task.Id);
        Assert.NotNull(detail);
        Assert.Equal("TREAD-01", detail!.EquipmentLabel);
        Assert.True(updated);
        Assert.Equal(taskId, maintenance.UpdatedTaskId);
        Assert.Equal(MaintenanceTaskStatus.InProgress, maintenance.UpdatedStatus);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Final1Presentation-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options, new TestGymContext());
    }

    private static Gym CreateGym(string code, string name, bool isActive)
    {
        return new Gym
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            AddressLine = "Test 1",
            City = "Tallinn",
            PostalCode = "10111",
            IsActive = isActive
        };
    }

    private sealed class TestGymContext : IGymContext
    {
        public Guid? GymId => null;
        public string? GymCode => null;
        public string? ActiveRole => null;
        public bool IgnoreGymFilter => true;
    }

    private sealed class StubUserContextService(Guid gymId, string gymCode, string roleName) : IUserContextService
    {
        public UserExecutionContext GetCurrent()
        {
            return new UserExecutionContext(
                UserId: Guid.NewGuid(),
                PersonId: Guid.NewGuid(),
                ActiveGymId: gymId,
                ActiveGymCode: gymCode,
                ActiveRole: roleName,
                AllRoles: [roleName],
                SystemRoles: []);
        }
    }

    private sealed class StubAuthorizationService(Member? currentMember = null, Staff? currentStaff = null) : IAuthorizationService
    {
        public Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles) => Task.FromResult(Guid.NewGuid());

        public Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles) =>
            Task.FromResult(Guid.NewGuid());

        public Task<Member?> GetCurrentMemberAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.FromResult(currentMember);

        public Task<Staff?> GetCurrentStaffAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.FromResult(currentStaff);

        public Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubMaintenanceWorkflowService(IReadOnlyCollection<MaintenanceTaskResponse> tasks) : IMaintenanceWorkflowService
    {
        public Guid? UpdatedTaskId { get; private set; }
        public MaintenanceTaskStatus? UpdatedStatus { get; private set; }

        public Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<EquipmentModelResponse> CreateEquipmentModelAsync(string gymCode, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteEquipmentModelAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode, EquipmentFilter? filter = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<EquipmentResponse> CreateEquipmentAsync(string gymCode, EquipmentUpsertRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<EquipmentResponse> UpdateEquipmentStatusAsync(string gymCode, Guid id, EquipmentStatusUpdateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteEquipmentAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode, MaintenanceTaskFilter? filter = null, CancellationToken cancellationToken = default) => Task.FromResult(tasks);

        public Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken = default)
        {
            UpdatedTaskId = taskId;
            UpdatedStatus = request.Status;
            return Task.FromResult(tasks.First(task => task.Id == taskId));
        }

        public Task<MaintenanceTaskResponse> UpdateTaskAssignmentAsync(string gymCode, Guid taskId, MaintenanceAssignmentUpdateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GenerateDueScheduledTasksAsync(string gymCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteMaintenanceTaskAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
