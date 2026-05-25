using SharedKernel.Persistence;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using App.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Gyms.Api;
using Modules.Gyms.Infrastructure.Persistence;
using Modules.Maintenance.Api;
using Modules.Maintenance.Infrastructure.Persistence;
using Modules.Memberships.Api;
using Modules.Memberships.Infrastructure.Persistence;
using Modules.Training.Api;
using Modules.Training.Infrastructure.Persistence;
using Modules.Users.Api;
using Modules.Users.Infrastructure.Persistence;

namespace Architecture.Tests;

[Trait("Category", "Architecture")]
public class ModuleDbContextOwnershipTests
{
    [Fact]
    public void ModuleRegistrations_RegisterOwningDbContexts_WhenConnectionStringIsConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=phase9;Username=postgres;Password=postgres",
            })
            .Build();
        var services = new ServiceCollection();

        services
            .AddUsersModule(configuration)
            .AddGymsModule(configuration)
            .AddMembershipsModule(configuration)
            .AddTrainingModule(configuration)
            .AddMaintenanceModule(configuration);

        AssertDbContextOptionsRegistered<UsersDbContext>(services);
        AssertDbContextOptionsRegistered<GymsDbContext>(services);
        AssertDbContextOptionsRegistered<MembershipsDbContext>(services);
        AssertDbContextOptionsRegistered<TrainingDbContext>(services);
        AssertDbContextOptionsRegistered<MaintenanceDbContext>(services);
    }

    [Fact]
    public void ModuleDbContexts_UseModuleDefaultSchemas()
    {
        using var users = new UsersDbContext(
            new DbContextOptionsBuilder<UsersDbContext>()
                .UseInMemoryDatabase($"UsersSchema-{Guid.NewGuid():N}")
                .Options);

        var gymId = Guid.NewGuid();
        using var gyms = CreateTenantContext<GymsDbContext>(
            gymId,
            options => new GymsDbContext(options, new TestGymContext(gymId)));
        using var memberships = CreateTenantContext<MembershipsDbContext>(
            gymId,
            options => new MembershipsDbContext(options, new TestGymContext(gymId)));
        using var training = CreateTenantContext<TrainingDbContext>(
            gymId,
            options => new TrainingDbContext(options, new TestGymContext(gymId)));
        using var maintenance = CreateTenantContext<MaintenanceDbContext>(
            gymId,
            options => new MaintenanceDbContext(options, new TestGymContext(gymId)));

        Assert.Equal("users", users.Model.GetDefaultSchema());
        Assert.Equal("gyms", gyms.Model.GetDefaultSchema());
        Assert.Equal("memberships", memberships.Model.GetDefaultSchema());
        Assert.Equal("training", training.Model.GetDefaultSchema());
        Assert.Equal("maintenance", maintenance.Model.GetDefaultSchema());
    }

    [Fact]
    public async Task UsersDbContext_CanWriteAndReadRefreshTokens()
    {
        await using var context = new UsersDbContext(
            new DbContextOptionsBuilder<UsersDbContext>()
                .UseInMemoryDatabase($"UsersDbContext-{Guid.NewGuid():N}")
                .Options);

        var userId = Guid.NewGuid();
        await context.RefreshTokens.AddAsync(new AppRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshToken = "refresh-token",
            Expiration = DateTime.UtcNow.AddDays(1),
        });

        await context.SaveChangesAsync();

        var token = await context.RefreshTokens.SingleAsync(entity => entity.UserId == userId);
        Assert.Equal("refresh-token", token.RefreshToken);
    }

    [Fact]
    public async Task GymsDbContext_CanWriteAndReadGymSettings()
    {
        var gymId = Guid.NewGuid();
        await using var context = CreateTenantContext<GymsDbContext>(
            gymId,
            options => new GymsDbContext(options, new TestGymContext(gymId)));

        await context.Gyms.AddAsync(new Gym
        {
            Id = gymId,
            Name = "Phase 9 Gym",
            Code = "phase-9",
            AddressLine = "Architecture 1",
            City = "Tallinn",
            PostalCode = "10111",
        });
        await context.GymSettings.AddAsync(new GymSettings
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            CurrencyCode = "EUR",
            TimeZone = "Europe/Tallinn",
        });

        await context.SaveChangesAsync();

        var settings = await context.GymSettings.SingleAsync();
        Assert.Equal(gymId, settings.GymId);
    }

    [Fact]
    public async Task MembershipsDbContext_CanWriteAndReadMembershipAggregate()
    {
        var gymId = Guid.NewGuid();
        await using var context = CreateTenantContext<MembershipsDbContext>(
            gymId,
            options => new MembershipsDbContext(options, new TestGymContext(gymId)));

        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Mari",
            LastName = "Member",
            PersonalCode = "P9-MEMBER",
        };
        var member = new Member
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Person = person,
            MemberCode = "M-900",
            Status = MemberStatus.Active,
        };
        var package = new MembershipPackage
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            PackageType = MembershipPackageType.Monthly,
            DurationValue = 1,
            DurationUnit = DurationUnit.Month,
            BasePrice = 49,
        };

        await context.Members.AddAsync(member);
        await context.MembershipPackages.AddAsync(package);
        await context.Memberships.AddAsync(new Membership
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Member = member,
            MembershipPackage = package,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            PriceAtPurchase = 49,
            Status = MembershipStatus.Active,
        });

        await context.SaveChangesAsync();

        var membership = await context.Memberships
            .Include(entity => entity.Member)
            .ThenInclude(entity => entity!.Person)
            .SingleAsync();
        Assert.Equal("Mari", membership.Member!.Person!.FirstName);
    }

    [Fact]
    public async Task TrainingDbContext_CanWriteAndReadTrainingSession()
    {
        var gymId = Guid.NewGuid();
        await using var context = CreateTenantContext<TrainingDbContext>(
            gymId,
            options => new TrainingDbContext(options, new TestGymContext(gymId)));

        var staff = new Staff
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            StaffCode = "T-900",
            Person = new Person
            {
                Id = Guid.NewGuid(),
                FirstName = "Tiina",
                LastName = "Trainer",
                PersonalCode = "P9-TRAINER",
            },
        };
        var category = new TrainingCategory
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
        };

        await context.Staff.AddAsync(staff);
        await context.TrainingCategories.AddAsync(category);
        await context.TrainingSessions.AddAsync(new TrainingSession
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Category = category,
            TrainerStaff = staff,
            StartAtUtc = DateTime.UtcNow.AddDays(1),
            EndAtUtc = DateTime.UtcNow.AddDays(1).AddHours(1),
            Capacity = 12,
            BasePrice = 10,
            Status = TrainingSessionStatus.Published,
        });

        await context.SaveChangesAsync();

        var session = await context.TrainingSessions
            .Include(entity => entity.TrainerStaff)
            .ThenInclude(entity => entity!.Person)
            .SingleAsync();
        Assert.Equal("Tiina", session.TrainerStaff!.Person!.FirstName);
    }

    [Fact]
    public async Task MaintenanceDbContext_CanWriteAndReadMaintenanceTask()
    {
        var gymId = Guid.NewGuid();
        await using var context = CreateTenantContext<MaintenanceDbContext>(
            gymId,
            options => new MaintenanceDbContext(options, new TestGymContext(gymId)));

        var model = new EquipmentModel
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Type = EquipmentType.Cardio,
            Manufacturer = "Phase9",
            MaintenanceIntervalDays = 30,
        };
        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            EquipmentModel = model,
            AssetTag = "EQ-900",
            CurrentStatus = EquipmentStatus.Active,
        };

        await context.EquipmentModels.AddAsync(model);
        await context.Equipment.AddAsync(equipment);
        await context.MaintenanceTasks.AddAsync(new MaintenanceTask
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Equipment = equipment,
            TaskType = MaintenanceTaskType.Scheduled,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceTaskStatus.Open,
        });

        await context.SaveChangesAsync();

        var task = await context.MaintenanceTasks
            .Include(entity => entity.Equipment)
            .ThenInclude(entity => entity!.EquipmentModel)
            .SingleAsync();
        Assert.Equal("EQ-900", task.Equipment!.AssetTag);
    }

    private static TContext CreateTenantContext<TContext>(
        Guid gymId,
        Func<DbContextOptions<TContext>, TContext> factory)
        where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase($"{typeof(TContext).Name}-{gymId:N}")
            .Options;

        return factory(options);
    }

    private static void AssertDbContextOptionsRegistered<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(DbContextOptions<TContext>));
    }

    private sealed class TestGymContext(Guid gymId) : IGymContext
    {
        public Guid? GymId { get; } = gymId;
        public string? GymCode => "phase-9";
        public string? ActiveRole => "GymAdmin";
        public bool IgnoreGymFilter => false;
    }
}
