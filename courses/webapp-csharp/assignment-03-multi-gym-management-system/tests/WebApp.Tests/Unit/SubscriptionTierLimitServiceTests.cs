using App.BLL.Exceptions;
using App.BLL.Services;
using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Unit;

public class SubscriptionTierLimitServiceTests
{
    [Fact]
    public async Task EnsureCanCreateMemberAsync_Throws_WhenStarterPlanLimitReached()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var gymId = Guid.NewGuid();
        await using var dbContext = new AppDbContext(options, new TestGymContext(gymId), new HttpContextAccessor());

        dbContext.Gyms.Add(new Gym
        {
            Id = gymId,
            Name = "Peak Forge",
            Code = "peak-forge",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        });

        dbContext.Subscriptions.Add(new Subscription
        {
            GymId = gymId,
            Plan = SubscriptionPlan.Starter,
            Status = SubscriptionStatus.Active,
            StartDate = new DateOnly(2026, 1, 1)
        });

        for (var i = 0; i < 60; i++)
        {
            dbContext.Members.Add(new Member
            {
                GymId = gymId,
                PersonId = Guid.NewGuid(),
                MemberCode = $"MEM-{i:D3}",
                Status = MemberStatus.Active
            });
        }

        await dbContext.SaveChangesAsync();

        var service = new SubscriptionTierLimitService(dbContext);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.EnsureCanCreateMemberAsync(gymId));
    }

    [Fact]
    public async Task EnsureCanCreateTrainingSessionAsync_DoesNotThrow_ForEnterprisePlan()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var gymId = Guid.NewGuid();
        await using var dbContext = new AppDbContext(options, new TestGymContext(gymId), new HttpContextAccessor());

        dbContext.Gyms.Add(new Gym
        {
            Id = gymId,
            Name = "Peak Forge",
            Code = "peak-forge",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        });

        dbContext.Subscriptions.Add(new Subscription
        {
            GymId = gymId,
            Plan = SubscriptionPlan.Enterprise,
            Status = SubscriptionStatus.Active,
            StartDate = new DateOnly(2026, 1, 1)
        });

        var category = new TrainingCategory
        {
            GymId = gymId,
            Name = "Strength"
        };

        dbContext.TrainingCategories.Add(category);

        for (var i = 0; i < 900; i++)
        {
            dbContext.TrainingSessions.Add(new TrainingSession
            {
                GymId = gymId,
                Category = category,
                Name = $"Session {i}",
                StartAtUtc = DateTime.UtcNow.AddDays(i + 1),
                EndAtUtc = DateTime.UtcNow.AddDays(i + 1).AddHours(1),
                Capacity = 10,
                BasePrice = 10,
                CurrencyCode = "EUR",
                Status = TrainingSessionStatus.Published
            });
        }

        await dbContext.SaveChangesAsync();

        var service = new SubscriptionTierLimitService(dbContext);
        await service.EnsureCanCreateTrainingSessionAsync(gymId);
    }

    private sealed class TestGymContext(Guid gymId) : IGymContext
    {
        public Guid? GymId => gymId;
        public string? GymCode => "peak-forge";
        public string? ActiveRole => RoleNames.GymAdmin;
        public bool IgnoreGymFilter => false;
    }
}
