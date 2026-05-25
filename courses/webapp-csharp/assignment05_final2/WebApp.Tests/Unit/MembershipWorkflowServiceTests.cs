using App.BLL.Contracts.Services;
using Modules.Gyms.Application.Authorization;
using Modules.Gyms.Application.Platform;
using Modules.Memberships.Application;
using WebApp.Areas.Admin.Queries;
using WebApp.Areas.Client.Queries;
using SharedKernel.Exceptions;
using Modules.Memberships.Application.Mappers;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using SharedKernel.Persistence;
using Modules.Maintenance.Infrastructure;
using Modules.Memberships.Infrastructure;
using Modules.Training.Application;
using Modules.Training.Infrastructure;
using Modules.Users.Infrastructure;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Shared.Contracts.ModuleApis;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Dtos.v1.Memberships;

namespace WebApp.Tests.Unit;

public class MembershipWorkflowServiceTests
{
    [Fact]
    public async Task SellMembershipAsync_ReturnsOverlapSuggestion_WhenMembershipAlreadyExists()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var gymId = Guid.NewGuid();
        await using var dbContext = new AppDbContext(options, new TestGymContext(gymId));

        var person = new Person { FirstName = "Test", LastName = "Member" };
        var member = new Member { GymId = gymId, Person = person, MemberCode = "MEM-1" };
        var package = new MembershipPackage
        {
            GymId = gymId,
            Name = "Monthly Pass",
            PackageType = MembershipPackageType.Monthly,
            DurationValue = 1,
            DurationUnit = DurationUnit.Month,
            BasePrice = 10m
        };

        dbContext.Members.Add(member);
        dbContext.MembershipPackages.Add(package);
        dbContext.Memberships.Add(new Membership
        {
            GymId = gymId,
            Member = member,
            MembershipPackage = package,
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 4, 30),
            PriceAtPurchase = 10m,
            Status = MembershipStatus.Active
        });
        dbContext.Gyms.Add(new Gym
        {
            Id = gymId,
            Name = "Test Gym",
            Code = "test-gym",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new TestAuthorizationService(gymId));

        var result = await service.SellMembershipAsync("test-gym", new Shared.Contracts.Dtos.v1.Memberships.SellMembershipRequest
        {
            MemberId = member.Id,
            MembershipPackageId = package.Id,
            RequestedStartDate = new DateOnly(2026, 4, 15)
        });

        Assert.True(result.OverlapDetected);
        Assert.Equal(new DateOnly(2026, 5, 1), result.SuggestedStartDate);
    }

    [Fact]
    public async Task UpdateMembershipStatusAsync_UpdatesStatus_ForValidTransition()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var gymId = Guid.NewGuid();
        await using var dbContext = new AppDbContext(options, new TestGymContext(gymId));

        var membership = new Membership
        {
            GymId = gymId,
            MemberId = Guid.NewGuid(),
            MembershipPackageId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 4, 30),
            PriceAtPurchase = 79m,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Active
        };

        dbContext.Gyms.Add(new Gym
        {
            Id = gymId,
            Name = "Test Gym",
            Code = "test-gym",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        });
        dbContext.Memberships.Add(membership);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new TestAuthorizationService(gymId));
        var result = await service.UpdateMembershipStatusAsync("test-gym", membership.Id, new MembershipStatusUpdateRequest
        {
            Status = MembershipStatus.Paused,
            Reason = "Vacation pause"
        });

        Assert.Equal(MembershipStatus.Paused, result.Status);
    }

    [Fact]
    public async Task UpdateMembershipStatusAsync_Throws_ForInvalidTransition()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var gymId = Guid.NewGuid();
        await using var dbContext = new AppDbContext(options, new TestGymContext(gymId));

        var membership = new Membership
        {
            GymId = gymId,
            MemberId = Guid.NewGuid(),
            MembershipPackageId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 30),
            PriceAtPurchase = 79m,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Pending
        };

        dbContext.Gyms.Add(new Gym
        {
            Id = gymId,
            Name = "Test Gym",
            Code = "test-gym",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        });
        dbContext.Memberships.Add(membership);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new TestAuthorizationService(gymId));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.UpdateMembershipStatusAsync(
            "test-gym",
            membership.Id,
            new MembershipStatusUpdateRequest { Status = MembershipStatus.Refunded }));
    }

    private static IMembershipWorkflowService CreateService(AppDbContext dbContext, IAuthorizationService authorizationService)
    {
        var unitOfWork = new AppUOW(dbContext);
        var memberRepository = new EfMemberRepository(dbContext);
        var membershipPackageRepository = new EfMembershipPackageRepository(dbContext);
        var membershipRepository = new EfMembershipRepository(dbContext);
        var paymentRepository = new EfPaymentRepository(dbContext);
        var mapper = new MembershipFinanceMapper();
        return new MembershipWorkflowService(
            new MembershipPackageService(dbContext, membershipPackageRepository, authorizationService, mapper),
            new MembershipService(
                dbContext,
                memberRepository,
                membershipPackageRepository,
                membershipRepository,
                paymentRepository,
                authorizationService,
                mapper),
            new PaymentService(
                dbContext,
                membershipRepository,
                paymentRepository,
                new TestTrainingModuleApi(dbContext),
                authorizationService,
                mapper),
            new BookingPricingService(dbContext));
    }

    private sealed class TestGymContext(Guid gymId) : IGymContext
    {
        public Guid? GymId => gymId;
        public string? GymCode => "test-gym";
        public string? ActiveRole => SharedKernel.RoleNames.GymAdmin;
        public bool IgnoreGymFilter => false;
    }

    private sealed class TestAuthorizationService(Guid gymId) : IAuthorizationService
    {
        public Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles) => Task.FromResult(gymId);
        public Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles) => Task.FromResult(gymId);
        public Task<Member?> GetCurrentMemberAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult<Member?>(null);
        public Task<Staff?> GetCurrentStaffAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult<Staff?>(null);
        public Task EnsureMemberSelfAccessAsync(Guid gymIdValue, Guid memberId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureBookingAccessAsync(Guid gymIdValue, Guid memberId, Guid trainingSessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestTrainingModuleApi(AppDbContext dbContext) : ITrainingModuleApi
    {
        public Task<StaffSummary?> GetStaffSummaryAsync(Guid gymId, Guid staffId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StaffSummary?>(null);

        public Task<TrainingSessionSummary?> GetTrainingSessionSummaryAsync(Guid gymId, Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TrainingSessionSummary?>(null);

        public async Task<BookingSummary?> GetBookingSummaryAsync(Guid gymId, Guid bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await dbContext.Bookings
                .AsNoTracking()
                .Where(entity => entity.GymId == gymId && entity.Id == bookingId)
                .Select(entity => new BookingSummary(entity.Id, entity.GymId, entity.MemberId, entity.TrainingSessionId))
                .FirstOrDefaultAsync(cancellationToken);

            return booking;
        }

        public async Task<IReadOnlyList<Guid>> ListBookingIdsForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Bookings
                .AsNoTracking()
                .Where(entity => entity.GymId == gymId && entity.MemberId == memberId)
                .Select(entity => entity.Id)
                .ToArrayAsync(cancellationToken);
        }
    }
}

