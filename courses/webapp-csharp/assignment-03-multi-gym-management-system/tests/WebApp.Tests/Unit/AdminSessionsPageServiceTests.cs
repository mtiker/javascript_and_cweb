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

public class AdminSessionsPageServiceTests
{
    [Fact]
    public async Task BuildAsync_TranslatesNameAndJoinsTrainerNames()
    {
        var gymId = Guid.NewGuid();
        var sessions = new[]
        {
            new AdminSessionRow(
                new LangStr { ["en"] = "Upper Body", ["et"] = "Ülakeha" },
                StartAtUtc: new DateTime(2026, 06, 01, 10, 0, 0, DateTimeKind.Utc),
                EndAtUtc: new DateTime(2026, 06, 01, 11, 0, 0, DateTimeKind.Utc),
                Capacity: 10,
                BookingCount: 4,
                Status: TrainingSessionStatus.Published,
                TrainerNames: new[] { "Anna Smith", "John Doe" })
        };

        var stub = new StubAdminSessionsQueryService(sessions);
        var service = new AdminSessionsPageService(stub);

        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("et-EE");
            var viewModel = await service.BuildAsync(gymId, "demo-gym");

            Assert.Equal(gymId, stub.LastGymId);
            Assert.Equal("demo-gym", viewModel.GymCode);
            var summary = Assert.Single(viewModel.Sessions);
            Assert.Equal("Ülakeha", summary.Name);
            Assert.Equal(10, summary.Capacity);
            Assert.Equal(4, summary.BookingCount);
            Assert.Equal(TrainingSessionStatus.Published, summary.Status);
            Assert.Equal("Anna Smith, John Doe", summary.TrainerNames);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public async Task BuildAsync_EmptyTrainerNamesProducesEmptyString()
    {
        var sessions = new[]
        {
            new AdminSessionRow(
                new LangStr("Cardio", "en"),
                StartAtUtc: DateTime.UtcNow,
                EndAtUtc: DateTime.UtcNow.AddHours(1),
                Capacity: 5,
                BookingCount: 0,
                Status: TrainingSessionStatus.Draft,
                TrainerNames: Array.Empty<string>())
        };

        var service = new AdminSessionsPageService(new StubAdminSessionsQueryService(sessions));
        var viewModel = await service.BuildAsync(Guid.NewGuid(), "g");

        Assert.Equal(string.Empty, Assert.Single(viewModel.Sessions).TrainerNames);
    }

    [Fact]
    public async Task QueryService_ProjectsCancelledBookingsOutAndDeduplicatesTrainers()
    {
        await using var dbContext = CreateDbContext();
        var gymId = await SeedSessionFixtureAsync(dbContext);
        var queryService = new AdminSessionsQueryService(new EfAppUnitOfWork(dbContext));

        var rows = await queryService.GetSessionsAsync(gymId);

        var row = Assert.Single(rows);
        Assert.Equal("Upper Body", row.Name.Translate("en"));
        Assert.Equal(1, row.BookingCount);
        var trainer = Assert.Single(row.TrainerNames);
        Assert.Equal("Anna Smith", trainer);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AdminSessions-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options, new TestGymContext(), new HttpContextAccessor());
    }

    private static async Task<Guid> SeedSessionFixtureAsync(AppDbContext dbContext)
    {
        var gym = new Gym
        {
            Name = "Sessions Gym",
            Code = "sessions-gym",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        };
        dbContext.Gyms.Add(gym);

        var category = new TrainingCategory
        {
            GymId = gym.Id,
            Name = new LangStr("Strength", "en")
        };
        var session = new TrainingSession
        {
            GymId = gym.Id,
            Category = category,
            Name = new LangStr("Upper Body", "en"),
            StartAtUtc = DateTime.UtcNow.AddHours(2),
            EndAtUtc = DateTime.UtcNow.AddHours(3),
            Capacity = 10,
            BasePrice = 0m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };

        var trainerPerson = new Person { FirstName = "Anna", LastName = "Smith" };
        var trainer = new Staff
        {
            GymId = gym.Id,
            Person = trainerPerson,
            StaffCode = "STAFF-T-001",
            Status = StaffStatus.Active
        };
        var jobRole = new JobRole
        {
            GymId = gym.Id,
            Code = "trainer",
            Title = new LangStr("Trainer", "en")
        };
        var contract = new EmploymentContract
        {
            GymId = gym.Id,
            Staff = trainer,
            PrimaryJobRole = jobRole,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
            WorkloadPercent = 50m,
            ContractStatus = ContractStatus.Active
        };
        var primaryShift = new WorkShift
        {
            GymId = gym.Id,
            Contract = contract,
            TrainingSession = session,
            ShiftType = ShiftType.Training,
            StartAtUtc = session.StartAtUtc.AddMinutes(-15),
            EndAtUtc = session.EndAtUtc.AddMinutes(15)
        };
        var duplicateShift = new WorkShift
        {
            GymId = gym.Id,
            Contract = contract,
            TrainingSession = session,
            ShiftType = ShiftType.Training,
            StartAtUtc = session.StartAtUtc.AddMinutes(-30),
            EndAtUtc = session.EndAtUtc.AddMinutes(30)
        };
        var nonTrainingShift = new WorkShift
        {
            GymId = gym.Id,
            Contract = contract,
            TrainingSession = session,
            ShiftType = ShiftType.Assisting,
            StartAtUtc = session.StartAtUtc.AddMinutes(-60),
            EndAtUtc = session.EndAtUtc.AddMinutes(60)
        };

        var memberPerson = new Person { FirstName = "Mem", LastName = "Ber" };
        var member = new Member
        {
            GymId = gym.Id,
            Person = memberPerson,
            MemberCode = "MEM-001",
            Status = MemberStatus.Active
        };
        var bookedBooking = new Booking
        {
            GymId = gym.Id,
            TrainingSession = session,
            Member = member,
            Status = BookingStatus.Booked,
            ChargedPrice = 0m,
            CurrencyCode = "EUR",
            PaymentRequired = false
        };
        var cancelledBooking = new Booking
        {
            GymId = gym.Id,
            TrainingSession = session,
            Member = member,
            Status = BookingStatus.Cancelled,
            ChargedPrice = 0m,
            CurrencyCode = "EUR",
            PaymentRequired = false
        };

        dbContext.TrainingCategories.Add(category);
        dbContext.TrainingSessions.Add(session);
        dbContext.People.AddRange(trainerPerson, memberPerson);
        dbContext.Staff.Add(trainer);
        dbContext.JobRoles.Add(jobRole);
        dbContext.EmploymentContracts.Add(contract);
        dbContext.WorkShifts.AddRange(primaryShift, duplicateShift, nonTrainingShift);
        dbContext.Members.Add(member);
        dbContext.Bookings.AddRange(bookedBooking, cancelledBooking);

        await dbContext.SaveChangesAsync();
        return gym.Id;
    }

    private sealed class StubAdminSessionsQueryService(IReadOnlyList<AdminSessionRow> rows) : IAdminSessionsQueryService
    {
        public Guid LastGymId { get; private set; }

        public Task<IReadOnlyList<AdminSessionRow>> GetSessionsAsync(Guid gymId, CancellationToken cancellationToken = default)
        {
            LastGymId = gymId;
            return Task.FromResult(rows);
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
