using System.Globalization;
using App.BLL.Services;
using App.BLL.Services.Client;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingSessions;
using WebApp.Areas.Client.Services;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class ClientSessionsPageServiceTests
{
    [Fact]
    public async Task BuildDetailsAsync_MapsCurrentBookingAndTrainerRosterPermission()
    {
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var startAtUtc = new DateTime(2026, 06, 01, 10, 0, 0, DateTimeKind.Utc);

        var trainingWorkflow = new DelegatingTrainingWorkflowService
        {
            GetSessionAsyncHandler = (gymCode, id, _) =>
            {
                Assert.Equal("peak-forge", gymCode);
                Assert.Equal(sessionId, id);
                return Task.FromResult(new TrainingSessionResponse
                {
                    Id = sessionId,
                    CategoryId = categoryId,
                    Name = "Upper Body",
                    Description = "Strength class",
                    StartAtUtc = startAtUtc,
                    EndAtUtc = startAtUtc.AddHours(1),
                    Capacity = 10,
                    BasePrice = 12m,
                    CurrencyCode = "EUR",
                    Status = TrainingSessionStatus.Published
                });
            }
        };
        var queryService = new StubClientSessionsQueryService(new ClientSessionDetailSnapshot(
            new LangStr { ["en"] = "Strength", ["et"] = "Joud" },
            ["Anna Smith"],
            new ClientSessionBookingState(bookingId, BookingStatus.Booked),
            CurrentStaffCanManageRoster: true));

        var service = CreateService(
            new UserExecutionContext(
                UserId: Guid.NewGuid(),
                PersonId: null,
                ActiveGymId: gymId,
                ActiveGymCode: "peak-forge",
                ActiveRole: RoleNames.Trainer,
                AllRoles: [RoleNames.Trainer],
                SystemRoles: []),
            new StubAuthorizationService(memberId, staffId),
            trainingWorkflow,
            queryService);

        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("et-EE");

            var result = await service.BuildDetailsAsync(sessionId);

            Assert.Equal(ClientSessionsPageStatus.Success, result.Status);
            Assert.Equal(gymId, queryService.LastDetailGymId);
            Assert.Equal(sessionId, queryService.LastDetailSessionId);
            Assert.Equal(memberId, queryService.LastDetailMemberId);
            Assert.Equal(staffId, queryService.LastDetailStaffId);

            var viewModel = Assert.IsType<WebApp.Models.SessionDetailPageViewModel>(result.ViewModel);
            Assert.Equal("peak-forge", viewModel.GymCode);
            Assert.Equal("Joud", viewModel.CategoryName);
            Assert.Equal(["Anna Smith"], viewModel.TrainerNames);
            Assert.Equal(memberId, viewModel.CurrentMemberId);
            Assert.Equal(bookingId, viewModel.CurrentBookingId);
            Assert.Equal(BookingStatus.Booked, viewModel.CurrentBookingStatus);
            Assert.True(viewModel.CanManageRoster);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public async Task BuildRosterAsync_ReturnsForbiddenWhenTrainerIsNotAssigned()
    {
        var gymId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var trainingWorkflowCalled = false;
        var queryService = new StubClientSessionsQueryService(
            DetailSnapshot: new ClientSessionDetailSnapshot(null, [], null, false),
            HasAssignment: false);
        var trainingWorkflow = new DelegatingTrainingWorkflowService
        {
            GetSessionAsyncHandler = (_, _, _) =>
            {
                trainingWorkflowCalled = true;
                return Task.FromResult(new TrainingSessionResponse());
            }
        };

        var service = CreateService(
            new UserExecutionContext(
                UserId: Guid.NewGuid(),
                PersonId: null,
                ActiveGymId: gymId,
                ActiveGymCode: "peak-forge",
                ActiveRole: RoleNames.Trainer,
                AllRoles: [RoleNames.Trainer],
                SystemRoles: []),
            new StubAuthorizationService(staffId: staffId),
            trainingWorkflow,
            queryService);

        var result = await service.BuildRosterAsync(sessionId);

        Assert.Equal(ClientSessionsPageStatus.Forbidden, result.Status);
        Assert.False(trainingWorkflowCalled);
        Assert.Equal(gymId, queryService.LastAssignmentGymId);
        Assert.Equal(sessionId, queryService.LastAssignmentSessionId);
        Assert.Equal(staffId, queryService.LastAssignmentStaffId);
    }

    [Fact]
    public async Task BookAsync_UsesCurrentMemberAndExistingTrainingWorkflow()
    {
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        BookingCreateRequest? capturedRequest = null;
        string? capturedGymCode = null;
        var trainingWorkflow = new DelegatingTrainingWorkflowService
        {
            CreateBookingAsyncHandler = (gymCode, request, _) =>
            {
                capturedGymCode = gymCode;
                capturedRequest = request;
                return Task.FromResult(new BookingResponse { Id = Guid.NewGuid() });
            }
        };
        var service = CreateService(
            new UserExecutionContext(
                UserId: Guid.NewGuid(),
                PersonId: null,
                ActiveGymId: gymId,
                ActiveGymCode: "peak-forge",
                ActiveRole: RoleNames.Member,
                AllRoles: [RoleNames.Member],
                SystemRoles: []),
            new StubAuthorizationService(memberId),
            trainingWorkflow,
            new StubClientSessionsQueryService(new ClientSessionDetailSnapshot(null, [], null, false)));

        var result = await service.BookAsync(sessionId, "PAY-123");

        Assert.Equal(ClientSessionCommandStatus.Success, result.Status);
        Assert.Equal("Booking confirmed.", result.Message);
        Assert.Equal("peak-forge", capturedGymCode);
        Assert.NotNull(capturedRequest);
        Assert.Equal(sessionId, capturedRequest!.TrainingSessionId);
        Assert.Equal(memberId, capturedRequest.MemberId);
        Assert.Equal("PAY-123", capturedRequest.PaymentReference);
    }

    private static ClientSessionsPageService CreateService(
        UserExecutionContext context,
        IAuthorizationService authorizationService,
        ITrainingWorkflowService trainingWorkflowService,
        IClientSessionsQueryService queryService)
    {
        return new ClientSessionsPageService(
            new StubUserContextService(context),
            authorizationService,
            trainingWorkflowService,
            queryService);
    }

    private sealed class StubUserContextService(UserExecutionContext context) : IUserContextService
    {
        public UserExecutionContext GetCurrent() => context;
    }

    private sealed class StubClientSessionsQueryService(
        ClientSessionDetailSnapshot DetailSnapshot,
        IReadOnlyList<ClientSessionRosterRow>? RosterRows = null,
        bool HasAssignment = true) : IClientSessionsQueryService
    {
        public Guid LastDetailGymId { get; private set; }
        public Guid LastDetailSessionId { get; private set; }
        public Guid? LastDetailMemberId { get; private set; }
        public Guid? LastDetailStaffId { get; private set; }
        public Guid LastAssignmentGymId { get; private set; }
        public Guid LastAssignmentSessionId { get; private set; }
        public Guid LastAssignmentStaffId { get; private set; }

        public Task<ClientSessionDetailSnapshot> GetDetailSnapshotAsync(
            Guid gymId,
            Guid sessionId,
            Guid? currentMemberId,
            Guid? currentStaffId,
            CancellationToken cancellationToken = default)
        {
            LastDetailGymId = gymId;
            LastDetailSessionId = sessionId;
            LastDetailMemberId = currentMemberId;
            LastDetailStaffId = currentStaffId;
            return Task.FromResult(DetailSnapshot);
        }

        public Task<IReadOnlyList<ClientSessionRosterRow>> GetRosterBookingsAsync(
            Guid gymId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RosterRows ?? Array.Empty<ClientSessionRosterRow>());
        }

        public Task<bool> HasTrainerAssignmentAsync(
            Guid gymId,
            Guid sessionId,
            Guid staffId,
            CancellationToken cancellationToken = default)
        {
            LastAssignmentGymId = gymId;
            LastAssignmentSessionId = sessionId;
            LastAssignmentStaffId = staffId;
            return Task.FromResult(HasAssignment);
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
