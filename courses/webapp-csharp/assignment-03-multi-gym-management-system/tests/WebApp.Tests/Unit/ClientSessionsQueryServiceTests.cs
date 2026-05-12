using App.BLL.Services.Client;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using App.DAL.EF.Tenant;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Unit;

public class ClientSessionsQueryServiceTests
{
    [Fact]
    public async Task GetDetailSnapshotAsync_ReturnsTenantScopedBookingAndTrainerData()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedSessionFixtureAsync(dbContext);
        var service = new ClientSessionsQueryService(new EfAppUnitOfWork(dbContext));

        var detail = await service.GetDetailSnapshotAsync(
            seed.GymId,
            seed.SessionId,
            seed.MemberId,
            seed.StaffId);
        var roster = await service.GetRosterBookingsAsync(seed.GymId, seed.SessionId);

        Assert.Equal("Strength", detail.CategoryName?.Translate("en"));
        Assert.Equal(seed.ActiveBookingId, detail.CurrentBooking?.BookingId);
        Assert.Equal(BookingStatus.Booked, detail.CurrentBooking?.Status);
        Assert.True(detail.CurrentStaffCanManageRoster);
        Assert.Equal(["Anna Smith"], detail.TrainerNames);

        Assert.Equal(2, roster.Count);
        Assert.Contains(roster, row => row.BookingId == seed.ActiveBookingId && row.MemberFirstName == "Ada");
        Assert.Contains(roster, row => row.Status == BookingStatus.Cancelled);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ClientSessions-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options, new TestGymContext(), new HttpContextAccessor());
    }

    private static async Task<SeedIds> SeedSessionFixtureAsync(AppDbContext dbContext)
    {
        var gym = new Gym
        {
            Name = "Client Sessions Gym",
            Code = "client-sessions",
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
            BasePrice = 12m,
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
        var firstShift = new WorkShift
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

        var memberPerson = new Person { FirstName = "Ada", LastName = "Lovelace" };
        var member = new Member
        {
            GymId = gym.Id,
            Person = memberPerson,
            MemberCode = "MEM-001",
            Status = MemberStatus.Active
        };
        var activeBooking = new Booking
        {
            GymId = gym.Id,
            TrainingSession = session,
            Member = member,
            Status = BookingStatus.Booked,
            ChargedPrice = 12m,
            CurrencyCode = "EUR",
            PaymentRequired = true
        };
        var cancelledBooking = new Booking
        {
            GymId = gym.Id,
            TrainingSession = session,
            Member = member,
            Status = BookingStatus.Cancelled,
            ChargedPrice = 12m,
            CurrencyCode = "EUR",
            PaymentRequired = true
        };

        dbContext.TrainingCategories.Add(category);
        dbContext.TrainingSessions.Add(session);
        dbContext.People.AddRange(trainerPerson, memberPerson);
        dbContext.Staff.Add(trainer);
        dbContext.JobRoles.Add(jobRole);
        dbContext.EmploymentContracts.Add(contract);
        dbContext.WorkShifts.AddRange(firstShift, duplicateShift);
        dbContext.Members.Add(member);
        dbContext.Bookings.AddRange(activeBooking, cancelledBooking);

        await dbContext.SaveChangesAsync();

        return new SeedIds(gym.Id, session.Id, member.Id, trainer.Id, activeBooking.Id);
    }

    private sealed record SeedIds(
        Guid GymId,
        Guid SessionId,
        Guid MemberId,
        Guid StaffId,
        Guid ActiveBookingId);

    private sealed class TestGymContext : IGymContext
    {
        public Guid? GymId => null;
        public string? GymCode => null;
        public string? ActiveRole => null;
        public bool IgnoreGymFilter => true;
    }
}
