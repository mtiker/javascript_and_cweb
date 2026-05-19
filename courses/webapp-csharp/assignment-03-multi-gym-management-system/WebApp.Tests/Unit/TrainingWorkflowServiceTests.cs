using System.Globalization;
using App.BLL.Exceptions;
using App.BLL.Mappers;
using App.BLL.Contracts.Services;
using App.BLL.Services;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using App.DAL.EF.Tenant;
using Base.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Bookings;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Unit;

public class TrainingWorkflowServiceTests
{
    private const string GymCode = "training-test-gym";

    [Fact]
    public async Task GetCategoriesAsync_ReturnsCategoriesOrderedByValidFrom()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var older = new TrainingCategory
        {
            GymId = gymId,
            Name = new LangStr("Older", "en"),
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-10))
        };
        var newer = new TrainingCategory
        {
            GymId = gymId,
            Name = new LangStr("Newer", "en"),
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1))
        };
        dbContext.TrainingCategories.AddRange(older, newer);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);

        var categories = (await service.GetCategoriesAsync(GymCode)).ToArray();

        Assert.Collection(
            categories,
            first => Assert.Equal("Older", first.Name),
            second => Assert.Equal("Newer", second.Name));
    }

    [Fact]
    public async Task CreateCategoryAsync_RejectsBlankName()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var service = CreateService(dbContext, gymId);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateCategoryAsync(GymCode, new TrainingCategoryUpsertRequest
            {
                Name = "   ",
                Description = "Bad"
            }));
    }

    [Fact]
    public async Task CreateCategoryAsync_PersistsThroughUnitOfWork()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var service = CreateService(dbContext, gymId);

        var response = await service.CreateCategoryAsync(GymCode, new TrainingCategoryUpsertRequest
        {
            Name = "  Strength  ",
            Description = "  Coach-led barbell  "
        });

        Assert.Equal("Strength", response.Name);
        Assert.Equal("Coach-led barbell", response.Description);

        var stored = await dbContext.TrainingCategories.SingleAsync();
        Assert.Equal(gymId, stored.GymId);
        Assert.Equal("Strength", stored.Name.Translate("en"));
    }

    [Fact]
    public async Task UpdateCategoryAsync_TenantScopedNotFound()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var foreignGymId = Guid.NewGuid();
        var foreign = new TrainingCategory
        {
            GymId = foreignGymId,
            Name = new LangStr("Foreign", "en")
        };
        dbContext.TrainingCategories.Add(foreign);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateCategoryAsync(GymCode, foreign.Id, new TrainingCategoryUpsertRequest
            {
                Name = "Update",
                Description = null
            }));
    }

    [Fact]
    public async Task DeleteCategoryAsync_RemovesCategory()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var category = new TrainingCategory
        {
            GymId = gymId,
            Name = new LangStr("ToDelete", "en")
        };
        dbContext.TrainingCategories.Add(category);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);

        await service.DeleteCategoryAsync(GymCode, category.Id);

        var existing = await dbContext.TrainingCategories.AnyAsync(entity => entity.Id == category.Id);
        Assert.False(existing);
    }

    [Fact]
    public async Task GetCategoriesAsync_TranslatesLangStrToActiveCulture()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var category = new TrainingCategory
        {
            GymId = gymId,
            Name = new LangStr
            {
                ["en"] = "Mobility",
                ["et"] = "Liikuvus"
            }
        };
        dbContext.TrainingCategories.Add(category);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("et-EE");
            var categories = (await service.GetCategoriesAsync(GymCode)).ToArray();

            var match = Assert.Single(categories);
            Assert.Equal("Liikuvus", match.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public async Task GetSessionsAsync_ReturnsSessionListWithTrainerStaff()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var seed = await SeedSessionFixtureAsync(dbContext, gymId);
        var service = CreateService(dbContext, gymId);

        var sessions = (await service.GetSessionsAsync(GymCode)).ToArray();

        var session = Assert.Single(sessions);
        Assert.Equal(seed.SessionId, session.Id);
        Assert.Equal("Upper Body", session.Name);
        Assert.Equal(seed.TrainerStaffId, session.TrainerStaffId);
        Assert.Equal("Train Coach", session.TrainerName);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsSessionDetailWithLocalizedCategory()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var seed = await SeedSessionFixtureAsync(dbContext, gymId);
        var service = CreateService(dbContext, gymId);

        var session = await service.GetSessionAsync(GymCode, seed.SessionId);

        Assert.Equal(seed.SessionId, session.Id);
        Assert.Equal("Upper Body", session.Name);
        Assert.Equal(seed.CategoryId, session.CategoryId);
        Assert.Equal(seed.TrainerStaffId, session.TrainerStaffId);
        Assert.Equal("Train Coach", session.TrainerName);
    }

    [Fact]
    public async Task CreateBookingAsync_PersistsBookingForPublishedSession()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var seed = await SeedBookingFixtureAsync(dbContext, gymId);
        var service = CreateService(dbContext, gymId);

        var response = await service.CreateBookingAsync(GymCode, new BookingCreateRequest
        {
            TrainingSessionId = seed.SessionId,
            MemberId = seed.MemberId,
            PaymentReference = null
        });

        Assert.Equal(seed.SessionId, response.TrainingSessionId);
        Assert.Equal(seed.MemberId, response.MemberId);
        Assert.Equal(BookingStatus.Booked, response.Status);

        var stored = await dbContext.Bookings.SingleAsync();
        Assert.Equal(seed.SessionId, stored.TrainingSessionId);
        Assert.Equal(seed.MemberId, stored.MemberId);
        Assert.Equal(BookingStatus.Booked, stored.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_RejectsDuplicateBookingForMember()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var seed = await SeedBookingFixtureAsync(dbContext, gymId);
        var service = CreateService(dbContext, gymId);

        await service.CreateBookingAsync(GymCode, new BookingCreateRequest
        {
            TrainingSessionId = seed.SessionId,
            MemberId = seed.MemberId
        });

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateBookingAsync(GymCode, new BookingCreateRequest
            {
                TrainingSessionId = seed.SessionId,
                MemberId = seed.MemberId
            }));
    }

    [Fact]
    public async Task CancelBookingAsync_StampsCancelledTimestamp()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var seed = await SeedBookingFixtureAsync(dbContext, gymId);
        var booking = new Booking
        {
            GymId = gymId,
            TrainingSessionId = seed.SessionId,
            MemberId = seed.MemberId,
            Status = BookingStatus.Booked,
            ChargedPrice = 0m,
            CurrencyCode = "EUR",
            PaymentRequired = false
        };
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);

        await service.CancelBookingAsync(GymCode, booking.Id);

        var stored = await dbContext.Bookings.SingleAsync(entity => entity.Id == booking.Id);
        Assert.Equal(BookingStatus.Cancelled, stored.Status);
        Assert.NotNull(stored.CancelledAtUtc);
    }

    [Fact]
    public async Task UpdateAttendanceAsync_AllowsAssignedTrainer()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var seed = await SeedBookingFixtureAsync(dbContext, gymId);
        var booking = new Booking
        {
            GymId = gymId,
            TrainingSessionId = seed.SessionId,
            MemberId = seed.MemberId,
            Status = BookingStatus.Booked,
            ChargedPrice = 0m,
            CurrencyCode = "EUR",
            PaymentRequired = false
        };
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);

        var response = await service.UpdateAttendanceAsync(GymCode, booking.Id, new AttendanceUpdateRequest
        {
            Status = BookingStatus.Attended
        });

        Assert.Equal(BookingStatus.Attended, response.Status);
        var stored = await dbContext.Bookings.SingleAsync(entity => entity.Id == booking.Id);
        Assert.Equal(BookingStatus.Attended, stored.Status);
    }

    [Fact]
    public async Task UpdateAttendanceAsync_RejectsTrainerForUnassignedSession()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var seed = await SeedBookingFixtureAsync(dbContext, gymId);
        var booking = new Booking
        {
            GymId = gymId,
            TrainingSessionId = seed.SessionId,
            MemberId = seed.MemberId,
            Status = BookingStatus.Booked,
            ChargedPrice = 0m,
            CurrencyCode = "EUR",
            PaymentRequired = false
        };
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        var authorization = new TestAuthorizationService(gymId)
        {
            EnsureTrainingAttendanceAccessThrows = true
        };
        var service = CreateService(dbContext, gymId, authorization);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateAttendanceAsync(GymCode, booking.Id, new AttendanceUpdateRequest
            {
                Status = BookingStatus.Attended
            }));
    }

    private static async Task<(AppDbContext dbContext, Guid gymId)> NewContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var gymId = Guid.NewGuid();
        var dbContext = new AppDbContext(options, new TestGymContext(gymId));

        dbContext.Gyms.Add(new Gym
        {
            Id = gymId,
            Name = "Training Test Gym",
            Code = GymCode,
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        });
        dbContext.GymSettings.Add(new GymSettings
        {
            GymId = gymId,
            CurrencyCode = "EUR",
            AllowNonMemberBookings = true,
            BookingCancellationHours = 6
        });
        await dbContext.SaveChangesAsync();
        return (dbContext, gymId);
    }

    private static async Task<(Guid SessionId, Guid MemberId)> SeedBookingFixtureAsync(AppDbContext dbContext, Guid gymId)
    {
        var category = new TrainingCategory
        {
            GymId = gymId,
            Name = new LangStr("Strength", "en")
        };
        var session = new TrainingSession
        {
            GymId = gymId,
            Category = category,
            Name = new LangStr("Upper Body", "en"),
            StartAtUtc = DateTime.UtcNow.AddDays(1),
            EndAtUtc = DateTime.UtcNow.AddDays(1).AddHours(1),
            Capacity = 10,
            BasePrice = 0m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };
        var person = new Person { FirstName = "Train", LastName = "Member" };
        var member = new Member
        {
            GymId = gymId,
            Person = person,
            MemberCode = "MEM-T-001",
            Status = MemberStatus.Active
        };
        dbContext.TrainingCategories.Add(category);
        dbContext.TrainingSessions.Add(session);
        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();
        return (session.Id, member.Id);
    }

    private static async Task<(Guid SessionId, Guid CategoryId, Guid TrainerStaffId)> SeedSessionFixtureAsync(AppDbContext dbContext, Guid gymId)
    {
        var category = new TrainingCategory
        {
            GymId = gymId,
            Name = new LangStr("Strength", "en")
        };
        var person = new Person { FirstName = "Train", LastName = "Coach" };
        var staff = new Staff
        {
            GymId = gymId,
            Person = person,
            StaffCode = "STAFF-T-001",
            Status = StaffStatus.Active
        };
        var session = new TrainingSession
        {
            GymId = gymId,
            Category = category,
            TrainerStaff = staff,
            TrainerStaffId = staff.Id,
            Name = new LangStr("Upper Body", "en"),
            StartAtUtc = DateTime.UtcNow.AddDays(1),
            EndAtUtc = DateTime.UtcNow.AddDays(1).AddHours(1),
            Capacity = 10,
            BasePrice = 0m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };

        dbContext.TrainingCategories.Add(category);
        dbContext.TrainingSessions.Add(session);
        dbContext.Staff.Add(staff);
        await dbContext.SaveChangesAsync();
        return (session.Id, category.Id, staff.Id);
    }

    private static ITrainingWorkflowService CreateService(
        AppDbContext dbContext,
        Guid gymId,
        IAuthorizationService? authorization = null)
    {
        var unitOfWork = new AppUOW(dbContext);
        var mapper = new TrainingMapper();
        return new TrainingWorkflowService(
            unitOfWork,
            authorization ?? new TestAuthorizationService(gymId),
            new TestUserContextService(),
            new TestMembershipWorkflowService(),
            new TestSubscriptionTierLimitService(),
            mapper);
    }

    private sealed class TestGymContext(Guid gymId) : IGymContext
    {
        public Guid? GymId => gymId;
        public string? GymCode => TrainingWorkflowServiceTests.GymCode;
        public string? ActiveRole => App.Domain.RoleNames.GymAdmin;
        public bool IgnoreGymFilter => false;
    }

    private sealed class TestAuthorizationService(Guid gymId) : IAuthorizationService
    {
        public bool EnsureMemberSelfAccessThrows { get; set; }
        public bool EnsureBookingAccessThrows { get; set; }
        public bool EnsureTrainingAttendanceAccessThrows { get; set; }

        public Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles) => Task.FromResult(gymId);
        public Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles) => Task.FromResult(gymId);
        public Task<Member?> GetCurrentMemberAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult<Member?>(null);
        public Task<Staff?> GetCurrentStaffAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult<Staff?>(null);

        public Task EnsureMemberSelfAccessAsync(Guid gymIdValue, Guid memberId, CancellationToken cancellationToken = default)
        {
            if (EnsureMemberSelfAccessThrows)
            {
                throw new ForbiddenException("Members can only access their own profile.");
            }
            return Task.CompletedTask;
        }

        public Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            if (EnsureBookingAccessThrows)
            {
                throw new ForbiddenException("Booking access denied.");
            }
            return Task.CompletedTask;
        }

        public Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default)
        {
            if (EnsureTrainingAttendanceAccessThrows)
            {
                throw new ForbiddenException("Trainer not assigned to session.");
            }
            return Task.CompletedTask;
        }

        public Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestUserContextService : IUserContextService
    {
        public UserExecutionContext GetCurrent() => new(
            UserId: Guid.NewGuid(),
            PersonId: Guid.NewGuid(),
            ActiveGymId: null,
            ActiveGymCode: GymCode,
            ActiveRole: App.Domain.RoleNames.GymAdmin,
            AllRoles: new[] { App.Domain.RoleNames.GymAdmin },
            SystemRoles: Array.Empty<string>());
    }

    private sealed class TestMembershipWorkflowService : IMembershipWorkflowService
    {
        public Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<MembershipPackageResponse>>(Array.Empty<MembershipPackageResponse>());
        public Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeletePackageAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<MembershipResponse>>(Array.Empty<MembershipResponse>());
        public Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MembershipResponse> UpdateMembershipStatusAsync(string gymCode, Guid id, MembershipStatusUpdateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PaymentResponse>>(Array.Empty<PaymentResponse>());
        public Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession, CancellationToken cancellationToken = default) => Task.FromResult(0m);
    }

    private sealed class TestSubscriptionTierLimitService : ISubscriptionTierLimitService
    {
        public Task EnsureCanCreateMemberAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureCanCreateStaffAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureCanCreateTrainingSessionAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureCanCreateEquipmentAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
