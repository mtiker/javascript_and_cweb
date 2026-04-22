using System.Security.Claims;
using App.BLL.Services;
using App.Domain.Entities;
using App.DTO.v1;
using App.DTO.v1.Bookings;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Members;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;
using App.DTO.v1.WorkShifts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Tests.Helpers;

public static class ControllerTestContextFactory
{
    public static T WithUser<T>(T controller, Guid? userId = null)
        where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString())
                ],
                authenticationType: "Test"))
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }
}

public static class ControllerAssert
{
    public static T AssertOk<T>(ActionResult<T> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<T>(ok.Value);
    }

    public static T AssertCreated<T>(ActionResult<T> result)
    {
        return result.Result switch
        {
            CreatedResult created => Assert.IsAssignableFrom<T>(created.Value),
            CreatedAtActionResult createdAtAction => Assert.IsAssignableFrom<T>(createdAtAction.Value),
            _ => throw new InvalidOperationException($"Expected CreatedResult or CreatedAtActionResult but got {result.Result?.GetType().Name ?? "null"}.")
        };
    }

    public static Message AssertMessage(ActionResult<Message> result, string expected)
    {
        var message = AssertOk(result);
        Assert.Contains(expected, message.Messages);
        return message;
    }
}

public sealed class DelegatingMemberWorkflowService : IMemberWorkflowService
{
    public Func<string, CancellationToken, Task<IReadOnlyCollection<MemberResponse>>> GetMembersAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<MemberResponse>>([]);

    public Func<string, CancellationToken, Task<MemberDetailResponse>> GetCurrentMemberAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<MemberDetailResponse>(new InvalidOperationException("GetCurrentMemberAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task<MemberDetailResponse>> GetMemberAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<MemberDetailResponse>(new InvalidOperationException("GetMemberAsyncHandler not configured."));

    public Func<string, MemberUpsertRequest, CancellationToken, Task<MemberDetailResponse>> CreateMemberAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<MemberDetailResponse>(new InvalidOperationException("CreateMemberAsyncHandler not configured."));

    public Func<string, Guid, MemberUpsertRequest, CancellationToken, Task<MemberDetailResponse>> UpdateMemberAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<MemberDetailResponse>(new InvalidOperationException("UpdateMemberAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteMemberAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<MemberResponse>> GetMembersAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetMembersAsyncHandler(gymCode, cancellationToken);

    public Task<MemberDetailResponse> GetCurrentMemberAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetCurrentMemberAsyncHandler(gymCode, cancellationToken);

    public Task<MemberDetailResponse> GetMemberAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        GetMemberAsyncHandler(gymCode, id, cancellationToken);

    public Task<MemberDetailResponse> CreateMemberAsync(string gymCode, MemberUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateMemberAsyncHandler(gymCode, request, cancellationToken);

    public Task<MemberDetailResponse> UpdateMemberAsync(string gymCode, Guid id, MemberUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateMemberAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteMemberAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteMemberAsyncHandler(gymCode, id, cancellationToken);
}

public sealed class DelegatingTrainingWorkflowService : ITrainingWorkflowService
{
    public Func<string, CancellationToken, Task<IReadOnlyCollection<BookingResponse>>> GetBookingsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<BookingResponse>>([]);

    public Func<string, BookingCreateRequest, CancellationToken, Task<BookingResponse>> CreateBookingAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<BookingResponse>(new InvalidOperationException("CreateBookingAsyncHandler not configured."));

    public Func<string, Guid, AttendanceUpdateRequest, CancellationToken, Task<BookingResponse>> UpdateAttendanceAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<BookingResponse>(new InvalidOperationException("UpdateAttendanceAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> CancelBookingAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<TrainingCategoryResponse>>([]);

    public Task<TrainingCategoryResponse> CreateCategoryAsync(string gymCode, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<TrainingCategoryResponse>(new InvalidOperationException("CreateCategoryAsync not configured."));

    public Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<TrainingCategoryResponse>(new InvalidOperationException("UpdateCategoryAsync not configured."));

    public Task DeleteCategoryAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<TrainingSessionResponse>>([]);

    public Task<TrainingSessionResponse> GetSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        Task.FromException<TrainingSessionResponse>(new InvalidOperationException("GetSessionAsync not configured."));

    public Task<TrainingSessionResponse> UpsertTrainingSessionAsync(string gymCode, Guid? sessionId, TrainingSessionUpsertRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<TrainingSessionResponse>(new InvalidOperationException("UpsertTrainingSessionAsync not configured."));

    public Task DeleteSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyCollection<WorkShiftResponse>> GetWorkShiftsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<WorkShiftResponse>>([]);

    public Task<WorkShiftResponse> CreateWorkShiftAsync(string gymCode, WorkShiftUpsertRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<WorkShiftResponse>(new InvalidOperationException("CreateWorkShiftAsync not configured."));

    public Task<WorkShiftResponse> UpdateWorkShiftAsync(string gymCode, Guid id, WorkShiftUpsertRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<WorkShiftResponse>(new InvalidOperationException("UpdateWorkShiftAsync not configured."));

    public Task DeleteWorkShiftAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyCollection<BookingResponse>> GetBookingsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetBookingsAsyncHandler(gymCode, cancellationToken);

    public Task<BookingResponse> CreateBookingAsync(string gymCode, BookingCreateRequest request, CancellationToken cancellationToken = default) =>
        CreateBookingAsyncHandler(gymCode, request, cancellationToken);

    public Task<BookingResponse> UpdateAttendanceAsync(string gymCode, Guid bookingId, AttendanceUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateAttendanceAsyncHandler(gymCode, bookingId, request, cancellationToken);

    public Task CancelBookingAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        CancelBookingAsyncHandler(gymCode, id, cancellationToken);
}

public sealed class DelegatingMembershipWorkflowService : IMembershipWorkflowService
{
    public Func<string, CancellationToken, Task<IReadOnlyCollection<MembershipResponse>>> GetMembershipsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<MembershipResponse>>([]);

    public Func<string, SellMembershipRequest, CancellationToken, Task<MembershipSaleResponse>> SellMembershipAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<MembershipSaleResponse>(new InvalidOperationException("SellMembershipAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteMembershipAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<MembershipPackageResponse>>([]);

    public Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<MembershipPackageResponse>(new InvalidOperationException("CreatePackageAsync not configured."));

    public Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<MembershipPackageResponse>(new InvalidOperationException("UpdatePackageAsync not configured."));

    public Task DeletePackageAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetMembershipsAsyncHandler(gymCode, cancellationToken);

    public Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default) =>
        SellMembershipAsyncHandler(gymCode, request, cancellationToken);

    public Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteMembershipAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<PaymentResponse>>([]);

    public Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<PaymentResponse>(new InvalidOperationException("CreatePaymentAsync not configured."));

    public Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession, CancellationToken cancellationToken = default) =>
        Task.FromResult(0m);
}
