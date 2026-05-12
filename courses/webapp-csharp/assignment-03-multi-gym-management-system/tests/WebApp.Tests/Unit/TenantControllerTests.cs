using App.Domain.Enums;
using App.DTO.v1.Bookings;
using App.DTO.v1.Members;
using App.DTO.v1.Memberships;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Mvc;
using Modules.GymManagement.Contracts;
using WebApp.ApiControllers.Tenant;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class TenantControllerTests
{
    private const string GymCode = "gym-alpha";

    [Fact]
    public async Task MembersController_ForwardsParametersAndReturnsCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var memberId = Guid.NewGuid();
        var listResponse = new[]
        {
            new MemberResponse
            {
                Id = memberId,
                MemberCode = "MEM-001",
                FullName = "Ada Lovelace",
                Status = MemberStatus.Active
            }
        };
        var detailResponse = CreateMemberDetail(memberId);
        var request = new MemberUpsertRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            MemberCode = "MEM-001",
            Status = MemberStatus.Active
        };

        var mediator = new RecordingMemberMediator(listResponse, detailResponse);

        var controller = ControllerTestContextFactory.WithUser(new MembersController(mediator));

        var list = await controller.GetMembers(GymCode, cancellationToken);
        Assert.Same(listResponse, ControllerAssert.AssertOk(list));
        var listMessage = Assert.IsType<ListMembersQuery>(mediator.SentRequests[^1]);
        Assert.Equal(GymCode, listMessage.GymCode);
        Assert.Equal(cancellationToken, mediator.SentCancellationTokens[^1]);

        var current = await controller.GetCurrentMember(GymCode, cancellationToken);
        Assert.Same(detailResponse, ControllerAssert.AssertOk(current));
        var currentMessage = Assert.IsType<GetCurrentMemberQuery>(mediator.SentRequests[^1]);
        Assert.Equal(GymCode, currentMessage.GymCode);
        Assert.Equal(cancellationToken, mediator.SentCancellationTokens[^1]);

        var get = await controller.GetMember(GymCode, memberId, cancellationToken);
        Assert.Same(detailResponse, ControllerAssert.AssertOk(get));
        var getMessage = Assert.IsType<GetMemberQuery>(mediator.SentRequests[^1]);
        Assert.Equal(GymCode, getMessage.GymCode);
        Assert.Equal(memberId, getMessage.MemberId);
        Assert.Equal(cancellationToken, mediator.SentCancellationTokens[^1]);

        var create = await controller.CreateMember(GymCode, request, cancellationToken);
        Assert.Same(detailResponse, ControllerAssert.AssertCreated(create));
        var createMessage = Assert.IsType<CreateMemberCommand>(mediator.SentRequests[^1]);
        Assert.Equal(GymCode, createMessage.GymCode);
        Assert.Same(request, createMessage.Request);
        Assert.Equal(cancellationToken, mediator.SentCancellationTokens[^1]);
        var createdAt = Assert.IsType<CreatedAtActionResult>(create.Result);
        Assert.Equal(nameof(MembersController.GetMember), createdAt.ActionName);
        Assert.Equal("1", createdAt.RouteValues?["version"]);
        Assert.Equal(GymCode, createdAt.RouteValues?["gymCode"]);
        Assert.Equal(memberId, createdAt.RouteValues?["id"]);

        var update = await controller.UpdateMember(GymCode, memberId, request, cancellationToken);
        Assert.Same(detailResponse, ControllerAssert.AssertOk(update));
        var updateMessage = Assert.IsType<UpdateMemberCommand>(mediator.SentRequests[^1]);
        Assert.Equal(GymCode, updateMessage.GymCode);
        Assert.Equal(memberId, updateMessage.MemberId);
        Assert.Same(request, updateMessage.Request);
        Assert.Equal(cancellationToken, mediator.SentCancellationTokens[^1]);

        var delete = await controller.DeleteMember(GymCode, memberId, cancellationToken);
        ControllerAssert.AssertNoContent(delete);
        var deleteMessage = Assert.IsType<DeleteMemberCommand>(mediator.SentRequests[^1]);
        Assert.Equal(GymCode, deleteMessage.GymCode);
        Assert.Equal(memberId, deleteMessage.MemberId);
        Assert.Equal(cancellationToken, mediator.SentCancellationTokens[^1]);
    }

    [Fact]
    public async Task BookingsController_ForwardsParametersAndReturnsCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var bookingId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var booking = new BookingResponse
        {
            Id = bookingId,
            MemberId = memberId,
            MemberCode = "MEM-001",
            MemberName = "Ada Lovelace",
            TrainingSessionId = sessionId,
            TrainingSessionName = "Strength Basics",
            Status = BookingStatus.Booked,
            ChargedPrice = 10m,
            PaymentRequired = true
        };
        var bookings = new[] { booking };
        var createRequest = new BookingCreateRequest
        {
            MemberId = memberId,
            TrainingSessionId = sessionId,
            PaymentReference = "PAY-1"
        };
        var attendanceRequest = new AttendanceUpdateRequest { Status = BookingStatus.Attended };

        var service = new DelegatingTrainingWorkflowService
        {
            GetBookingsAsyncHandler = (gymCode, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<BookingResponse>>(bookings);
            },
            CreateBookingAsyncHandler = (gymCode, forwardedRequest, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Same(createRequest, forwardedRequest);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(booking);
            },
            UpdateAttendanceAsyncHandler = (gymCode, id, forwardedRequest, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(bookingId, id);
                Assert.Same(attendanceRequest, forwardedRequest);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(booking);
            },
            CancelBookingAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(bookingId, id);
                Assert.Equal(cancellationToken, token);
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(new BookingsController(new TrainingWorkflowMediatorAdapter(service)));

        var list = await controller.GetBookings(GymCode, cancellationToken);
        Assert.Same(bookings, ControllerAssert.AssertOk(list));

        var create = await controller.CreateBooking(GymCode, createRequest, cancellationToken);
        Assert.Same(booking, ControllerAssert.AssertCreated(create));

        var update = await controller.UpdateAttendance(GymCode, bookingId, attendanceRequest, cancellationToken);
        Assert.Same(booking, ControllerAssert.AssertOk(update));

        var cancel = await controller.CancelBooking(GymCode, bookingId, cancellationToken);
        ControllerAssert.AssertNoContent(cancel);
    }

    [Fact]
    public async Task MembershipsController_ForwardsParametersAndReturnsCurrentResultShapes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var membershipId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var membership = new MembershipResponse
        {
            Id = membershipId,
            MemberId = memberId,
            MembershipPackageId = packageId,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
            PriceAtPurchase = 45m,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Active
        };
        var memberships = new[] { membership };
        var request = new SellMembershipRequest
        {
            MemberId = memberId,
            MembershipPackageId = packageId,
            RequestedStartDate = new DateOnly(2026, 1, 1),
            PaymentReference = "PAY-2"
        };
        var statusRequest = new MembershipStatusUpdateRequest
        {
            Status = MembershipStatus.Paused,
            Reason = "Freeze"
        };
        var updatedMembership = new MembershipResponse
        {
            Id = membershipId,
            MemberId = memberId,
            MembershipPackageId = packageId,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            PriceAtPurchase = membership.PriceAtPurchase,
            CurrencyCode = membership.CurrencyCode,
            Status = MembershipStatus.Paused
        };
        var sale = new MembershipSaleResponse
        {
            MembershipId = membershipId,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate
        };

        var service = new DelegatingMembershipWorkflowService
        {
            GetMembershipsAsyncHandler = (gymCode, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult<IReadOnlyCollection<MembershipResponse>>(memberships);
            },
            SellMembershipAsyncHandler = (gymCode, forwardedRequest, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Same(request, forwardedRequest);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(sale);
            },
            UpdateMembershipStatusAsyncHandler = (gymCode, id, forwardedRequest, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(membershipId, id);
                Assert.Same(statusRequest, forwardedRequest);
                Assert.Equal(cancellationToken, token);
                return Task.FromResult(updatedMembership);
            },
            DeleteMembershipAsyncHandler = (gymCode, id, token) =>
            {
                Assert.Equal(GymCode, gymCode);
                Assert.Equal(membershipId, id);
                Assert.Equal(cancellationToken, token);
                return Task.CompletedTask;
            }
        };

        var controller = ControllerTestContextFactory.WithUser(new MembershipsController(new MembershipFinanceMediatorAdapter(service)));

        var list = await controller.GetMemberships(GymCode, cancellationToken);
        Assert.Same(memberships, ControllerAssert.AssertOk(list));

        var sell = await controller.SellMembership(GymCode, request, cancellationToken);
        Assert.Same(sale, ControllerAssert.AssertOk(sell));

        var updateStatus = await controller.UpdateMembershipStatus(GymCode, membershipId, statusRequest, cancellationToken);
        Assert.Same(updatedMembership, ControllerAssert.AssertOk(updateStatus));

        var delete = await controller.DeleteMembership(GymCode, membershipId, cancellationToken);
        ControllerAssert.AssertMessage(delete, "Membership deleted.");
    }

    private static MemberDetailResponse CreateMemberDetail(Guid memberId) =>
        new()
        {
            Id = memberId,
            MemberCode = "MEM-001",
            FirstName = "Ada",
            LastName = "Lovelace",
            FullName = "Ada Lovelace",
            Status = MemberStatus.Active
        };

    private sealed class RecordingMemberMediator(
        IReadOnlyCollection<MemberResponse> listResponse,
        MemberDetailResponse detailResponse) : IMediator
    {
        public List<object> SentRequests { get; } = [];
        public List<CancellationToken> SentCancellationTokens { get; } = [];

        public Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            SentCancellationTokens.Add(cancellationToken);

            if (request is not DeleteMemberCommand)
            {
                throw new InvalidOperationException($"Unexpected mediator request {request.GetType().FullName}.");
            }

            return Task.CompletedTask;
        }

        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            SentCancellationTokens.Add(cancellationToken);

            object response = request switch
            {
                ListMembersQuery => listResponse,
                GetCurrentMemberQuery or GetMemberQuery or CreateMemberCommand or UpdateMemberCommand => detailResponse,
                _ => throw new InvalidOperationException($"Unexpected mediator request {request.GetType().FullName}.")
            };

            return Task.FromResult((TResponse)response);
        }
    }
}
