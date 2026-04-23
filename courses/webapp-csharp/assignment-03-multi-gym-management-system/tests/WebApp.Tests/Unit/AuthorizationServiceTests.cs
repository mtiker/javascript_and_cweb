using App.BLL.Exceptions;
using App.BLL.Services;
using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Unit;

public class AuthorizationServiceTests
{
    [Fact]
    public async Task EnsureTenantAccessAsync_RejectsWhenActiveGymContextIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var gym = CreateGym("alpha");
        dbContext.Gyms.Add(gym);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new UserExecutionContext(
            UserId: Guid.NewGuid(),
            PersonId: null,
            ActiveGymId: null,
            ActiveGymCode: null,
            ActiveRole: null,
            AllRoles: [],
            SystemRoles: []));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.EnsureTenantAccessAsync(gym.Code, CancellationToken.None, RoleNames.GymAdmin));
    }

    [Fact]
    public async Task EnsureTenantAccessAsync_RejectsWhenRouteGymDiffersFromActiveGym()
    {
        await using var dbContext = CreateDbContext();
        var activeGym = CreateGym("alpha");
        var routeGym = CreateGym("beta");
        dbContext.Gyms.AddRange(activeGym, routeGym);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, CreateContext(
            activeGym.Id,
            activeGym.Code,
            personId: Guid.NewGuid(),
            RoleNames.GymAdmin));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.EnsureTenantAccessAsync(routeGym.Code, CancellationToken.None, RoleNames.GymAdmin));
    }

    [Fact]
    public async Task EnsureTenantAccessAsync_RejectsWhenAllowedRoleIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var gym = CreateGym("alpha");
        dbContext.Gyms.Add(gym);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, CreateContext(
            gym.Id,
            gym.Code,
            personId: Guid.NewGuid(),
            RoleNames.Member));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.EnsureTenantAccessAsync(gym.Code, CancellationToken.None, RoleNames.GymOwner, RoleNames.GymAdmin));
    }

    [Fact]
    public async Task EnsureMemberSelfAccessAsync_AllowsOwnMemberAndRejectsOthers()
    {
        await using var dbContext = CreateDbContext();
        var gym = CreateGym("alpha");
        var ownPersonId = Guid.NewGuid();
        var ownMember = CreateMember(gym.Id, ownPersonId, "MEM-SELF");
        var otherMember = CreateMember(gym.Id, Guid.NewGuid(), "MEM-OTHER");

        dbContext.Gyms.Add(gym);
        dbContext.Members.AddRange(ownMember, otherMember);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, CreateContext(
            gym.Id,
            gym.Code,
            ownPersonId,
            RoleNames.Member));

        await service.EnsureMemberSelfAccessAsync(gym.Id, ownMember.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.EnsureMemberSelfAccessAsync(gym.Id, otherMember.Id, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureBookingAccessAsync_TrainerMustBeAssignedToSession()
    {
        await using var dbContext = CreateDbContext();
        var gym = CreateGym("alpha");
        var trainerPersonId = Guid.NewGuid();
        var trainer = CreateStaff(gym.Id, trainerPersonId, "STF-TRAINER");
        var otherTrainer = CreateStaff(gym.Id, Guid.NewGuid(), "STF-OTHER");
        var jobRole = CreateJobRole(gym.Id, "trainer");
        var trainerContract = CreateContract(gym.Id, trainer, jobRole);
        var category = CreateCategory(gym.Id);
        var assignedSession = CreateTrainingSession(gym.Id, category, "Assigned");
        var unassignedSession = CreateTrainingSession(gym.Id, category, "Unassigned");
        var member = CreateMember(gym.Id, Guid.NewGuid(), "MEM-BOOK");
        var assignedBooking = CreateBooking(gym.Id, member, assignedSession);
        var unassignedBooking = CreateBooking(gym.Id, member, unassignedSession);
        var assignedShift = new WorkShift
        {
            GymId = gym.Id,
            Contract = trainerContract,
            TrainingSession = assignedSession,
            ShiftType = ShiftType.Training,
            StartAtUtc = assignedSession.StartAtUtc,
            EndAtUtc = assignedSession.EndAtUtc
        };

        dbContext.Gyms.Add(gym);
        dbContext.Staff.AddRange(trainer, otherTrainer);
        dbContext.JobRoles.Add(jobRole);
        dbContext.EmploymentContracts.Add(trainerContract);
        dbContext.TrainingCategories.Add(category);
        dbContext.TrainingSessions.AddRange(assignedSession, unassignedSession);
        dbContext.Members.Add(member);
        dbContext.Bookings.AddRange(assignedBooking, unassignedBooking);
        dbContext.WorkShifts.Add(assignedShift);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, CreateContext(
            gym.Id,
            gym.Code,
            trainerPersonId,
            RoleNames.Trainer));

        await service.EnsureBookingAccessAsync(assignedBooking, CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.EnsureBookingAccessAsync(unassignedBooking, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureMaintenanceTaskAccessAsync_CaretakerMustBeAssigned()
    {
        await using var dbContext = CreateDbContext();
        var gym = CreateGym("alpha");
        var caretakerPersonId = Guid.NewGuid();
        var caretaker = CreateStaff(gym.Id, caretakerPersonId, "STF-CARETAKER");
        var otherStaff = CreateStaff(gym.Id, Guid.NewGuid(), "STF-OTHER");
        var model = CreateEquipmentModel(gym.Id, "Model-1");
        var equipment = CreateEquipment(gym.Id, model, "EQ-1", "SN-1");
        var assignedTask = new MaintenanceTask
        {
            GymId = gym.Id,
            Equipment = equipment,
            AssignedStaff = caretaker,
            TaskType = MaintenanceTaskType.Scheduled
        };
        var unassignedTask = new MaintenanceTask
        {
            GymId = gym.Id,
            Equipment = equipment,
            AssignedStaff = otherStaff,
            TaskType = MaintenanceTaskType.Scheduled
        };

        dbContext.Gyms.Add(gym);
        dbContext.Staff.AddRange(caretaker, otherStaff);
        dbContext.EquipmentModels.Add(model);
        dbContext.Equipment.Add(equipment);
        dbContext.MaintenanceTasks.AddRange(assignedTask, unassignedTask);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, CreateContext(
            gym.Id,
            gym.Code,
            caretakerPersonId,
            RoleNames.Caretaker));

        await service.EnsureMaintenanceTaskAccessAsync(assignedTask, CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.EnsureMaintenanceTaskAccessAsync(unassignedTask, CancellationToken.None));
    }

    private static IAuthorizationService CreateService(AppDbContext dbContext, UserExecutionContext context)
    {
        var userContextService = new TestUserContextService(context);
        var currentActorResolver = new CurrentActorResolver(dbContext, userContextService);

        return new AuthorizationService(
            currentActorResolver,
            new TenantAccessChecker(dbContext, currentActorResolver),
            new ResourceAuthorizationChecker(dbContext, currentActorResolver));
    }

    private static UserExecutionContext CreateContext(Guid gymId, string gymCode, Guid? personId, params string[] roles) =>
        new(
            UserId: Guid.NewGuid(),
            PersonId: personId,
            ActiveGymId: gymId,
            ActiveGymCode: gymCode,
            ActiveRole: roles.FirstOrDefault(),
            AllRoles: roles,
            SystemRoles: roles.Where(RoleNames.SystemRoles.Contains).ToArray());

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AuthorizationService-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(
            options,
            new TestGymContext(),
            new HttpContextAccessor());
    }

    private static Gym CreateGym(string code) =>
        new()
        {
            Name = $"Gym {code}",
            Code = code,
            AddressLine = "Demo street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        };

    private static Member CreateMember(Guid gymId, Guid personId, string memberCode) =>
        new()
        {
            GymId = gymId,
            MemberCode = memberCode,
            PersonId = personId,
            Person = new Person
            {
                Id = personId,
                FirstName = "Test",
                LastName = memberCode,
                PersonalCode = $"{Guid.NewGuid():N}"[..11]
            }
        };

    private static Staff CreateStaff(Guid gymId, Guid personId, string staffCode) =>
        new()
        {
            GymId = gymId,
            StaffCode = staffCode,
            PersonId = personId,
            Person = new Person
            {
                Id = personId,
                FirstName = "Staff",
                LastName = staffCode,
                PersonalCode = $"{Guid.NewGuid():N}"[..11]
            }
        };

    private static JobRole CreateJobRole(Guid gymId, string code) =>
        new()
        {
            GymId = gymId,
            Code = code,
            Title = new LangStr("Role", "en")
        };

    private static EmploymentContract CreateContract(Guid gymId, Staff staff, JobRole jobRole) =>
        new()
        {
            GymId = gymId,
            Staff = staff,
            PrimaryJobRole = jobRole,
            WorkloadPercent = 100m,
            ContractStatus = ContractStatus.Active
        };

    private static TrainingCategory CreateCategory(Guid gymId) =>
        new()
        {
            GymId = gymId,
            Name = new LangStr("Strength", "en")
        };

    private static TrainingSession CreateTrainingSession(Guid gymId, TrainingCategory category, string name) =>
        new()
        {
            GymId = gymId,
            Category = category,
            Name = new LangStr(name, "en"),
            StartAtUtc = DateTime.UtcNow.AddDays(1),
            EndAtUtc = DateTime.UtcNow.AddDays(1).AddHours(1),
            Capacity = 10,
            BasePrice = 10m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };

    private static Booking CreateBooking(Guid gymId, Member member, TrainingSession session) =>
        new()
        {
            GymId = gymId,
            Member = member,
            TrainingSession = session,
            Status = BookingStatus.Booked,
            ChargedPrice = 0m
        };

    private static EquipmentModel CreateEquipmentModel(Guid gymId, string name) =>
        new()
        {
            GymId = gymId,
            Name = name,
            Type = EquipmentType.Cardio,
            MaintenanceIntervalDays = 90
        };

    private static Equipment CreateEquipment(Guid gymId, EquipmentModel model, string assetTag, string serialNumber) =>
        new()
        {
            GymId = gymId,
            EquipmentModel = model,
            AssetTag = assetTag,
            SerialNumber = serialNumber,
            CurrentStatus = EquipmentStatus.Active
        };

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
}
