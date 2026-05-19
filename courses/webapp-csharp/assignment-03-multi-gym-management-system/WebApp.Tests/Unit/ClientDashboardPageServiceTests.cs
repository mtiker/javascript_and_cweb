using System.Globalization;
using App.BLL.Contracts.Services;
using App.BLL.Services;
using App.BLL.Contracts.Services.Client;
using App.BLL.Services.Client;
using Base.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using WebApp.Areas.Client.Services;

namespace WebApp.Tests.Unit;

public class ClientDashboardPageServiceTests
{
    [Fact]
    public async Task BuildAsync_ReturnsNullWhenActiveGymContextIsMissing()
    {
        var queryService = new StubClientDashboardQueryService(EmptySnapshot());
        var service = new ClientDashboardPageService(
            new StubUserContextService(new UserExecutionContext(
                UserId: Guid.NewGuid(),
                PersonId: null,
                ActiveGymId: null,
                ActiveGymCode: null,
                ActiveRole: null,
                AllRoles: [],
                SystemRoles: [])),
            new StubAuthorizationService(),
            queryService);

        var viewModel = await service.BuildAsync();

        Assert.Null(viewModel);
        Assert.Equal(0, queryService.CallCount);
    }

    [Fact]
    public async Task BuildAsync_MapsDashboardSnapshotToCurrentCultureViewModel()
    {
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var equipmentId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var createdByStaffId = Guid.NewGuid();
        var startAtUtc = new DateTime(2026, 06, 01, 10, 0, 0, DateTimeKind.Utc);
        var dueAtUtc = new DateTime(2026, 06, 02, 12, 0, 0, DateTimeKind.Utc);

        var snapshot = new ClientDashboardSnapshot(
            UpcomingSessions:
            [
                new ClientDashboardSessionRow(
                    sessionId,
                    categoryId,
                    new LangStr { ["en"] = "Yoga", ["et"] = "Jooga" },
                    new LangStr { ["en"] = "Description", ["et"] = "Kirjeldus" },
                    startAtUtc,
                    startAtUtc.AddHours(1),
                    Capacity: 12,
                    BasePrice: 9.99m,
                    CurrencyCode: "EUR",
                    TrainingSessionStatus.Published)
            ],
            MyBookings:
            [
                new ClientDashboardBookingRow(
                    bookingId,
                    sessionId,
                    new LangStr { ["en"] = "Yoga", ["et"] = "Jooga" },
                    memberId,
                    "Ada",
                    "Lovelace",
                    "MEM-001",
                    BookingStatus.Booked,
                    ChargedPrice: 9.99m,
                    PaymentRequired: true)
            ],
            AssignedTasks:
            [
                new ClientDashboardMaintenanceTaskRow(
                    taskId,
                    equipmentId,
                    "EQ-001",
                    new LangStr { ["en"] = "Treadmill", ["et"] = "Jooksulint" },
                    staffId,
                    "Care",
                    "Taker",
                    createdByStaffId,
                    MaintenanceTaskType.Scheduled,
                    MaintenancePriority.High,
                    MaintenanceTaskStatus.Open,
                    dueAtUtc,
                    StartedAtUtc: null,
                    CompletedAtUtc: null,
                    Notes: "Check belt")
            ]);

        var queryService = new StubClientDashboardQueryService(snapshot);
        var service = new ClientDashboardPageService(
            new StubUserContextService(new UserExecutionContext(
                UserId: Guid.NewGuid(),
                PersonId: null,
                ActiveGymId: gymId,
                ActiveGymCode: "peak-forge",
                ActiveRole: "Member",
                AllRoles: ["Member"],
                SystemRoles: [])),
            new StubAuthorizationService(memberId, staffId),
            queryService);

        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("et-EE");

            var viewModel = await service.BuildAsync();

            Assert.NotNull(viewModel);
            Assert.Equal("peak-forge", viewModel!.ActiveGymCode);
            Assert.Equal("Member", viewModel.ActiveRole);
            Assert.Equal(gymId, queryService.LastGymId);
            Assert.Equal(memberId, queryService.LastMemberId);
            Assert.Equal(staffId, queryService.LastStaffId);

            var session = Assert.Single(viewModel.UpcomingSessions);
            Assert.Equal(sessionId, session.Id);
            Assert.Equal("Jooga", session.Name);
            Assert.Equal("Kirjeldus", session.Description);
            Assert.Equal(12, session.Capacity);
            Assert.Equal(TrainingSessionStatus.Published, session.Status);

            var booking = Assert.Single(viewModel.MyBookings);
            Assert.Equal(bookingId, booking.Id);
            Assert.Equal("Jooga", booking.TrainingSessionName);
            Assert.Equal("Ada Lovelace", booking.MemberName);
            Assert.Equal("MEM-001", booking.MemberCode);
            Assert.True(booking.PaymentRequired);

            var task = Assert.Single(viewModel.AssignedTasks);
            Assert.Equal(taskId, task.Id);
            Assert.Equal("EQ-001", task.EquipmentAssetTag);
            Assert.Equal("Jooksulint", task.EquipmentName);
            Assert.Equal("Care Taker", task.AssignedStaffName);
            Assert.Equal(createdByStaffId, task.CreatedByStaffId);
            Assert.Equal(MaintenancePriority.High, task.Priority);
            Assert.Equal("Check belt", task.Notes);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    private static ClientDashboardSnapshot EmptySnapshot() =>
        new(
            UpcomingSessions: Array.Empty<ClientDashboardSessionRow>(),
            MyBookings: Array.Empty<ClientDashboardBookingRow>(),
            AssignedTasks: Array.Empty<ClientDashboardMaintenanceTaskRow>());

    private sealed class StubUserContextService(UserExecutionContext context) : IUserContextService
    {
        public UserExecutionContext GetCurrent() => context;
    }

    private sealed class StubClientDashboardQueryService(ClientDashboardSnapshot snapshot) : IClientDashboardQueryService
    {
        public int CallCount { get; private set; }
        public Guid LastGymId { get; private set; }
        public Guid? LastMemberId { get; private set; }
        public Guid? LastStaffId { get; private set; }

        public Task<ClientDashboardSnapshot> GetSnapshotAsync(
            Guid gymId,
            Guid? currentMemberId,
            Guid? currentStaffId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastGymId = gymId;
            LastMemberId = currentMemberId;
            LastStaffId = currentStaffId;
            return Task.FromResult(snapshot);
        }
    }

    private sealed class StubAuthorizationService(Guid? memberId = null, Guid? staffId = null) : IAuthorizationService
    {
        public Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles) =>
            throw new NotSupportedException();

        public Task<Guid> EnsureTenantAccessAsync(
            string gymCode,
            CancellationToken cancellationToken,
            params string[] allowedRoles) =>
            throw new NotSupportedException();

        public Task<Member?> GetCurrentMemberAsync(Guid gymId, CancellationToken cancellationToken = default) =>
            Task.FromResult(memberId.HasValue ? new Member { Id = memberId.Value, GymId = gymId } : null);

        public Task<Staff?> GetCurrentStaffAsync(Guid gymId, CancellationToken cancellationToken = default) =>
            Task.FromResult(staffId.HasValue ? new Staff { Id = staffId.Value, GymId = gymId } : null);

        public Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
