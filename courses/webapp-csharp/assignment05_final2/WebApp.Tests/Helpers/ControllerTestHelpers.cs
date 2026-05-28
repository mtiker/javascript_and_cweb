using System.Security.Claims;
using App.BLL.Contracts.Services;
using Modules.Gyms.Application.Authorization;
using Modules.Gyms.Application.Platform;
using Modules.Memberships.Application;
using WebApp.Areas.Admin.Queries;
using WebApp.Areas.Client.Queries;
using App.Domain.Entities;
using Shared.Contracts.Dtos.v1;
using Shared.Contracts.Dtos.v1.Bookings;
using Shared.Contracts.Dtos.v1.Equipment;
using Shared.Contracts.Dtos.v1.EquipmentModels;
using Shared.Contracts.Dtos.v1.GymSettings;
using Shared.Contracts.Dtos.v1.GymUsers;
using Shared.Contracts.Dtos.v1.Identity;
using Shared.Contracts.Dtos.v1.MaintenanceTasks;
using Shared.Contracts.Dtos.v1.MemberWorkspace;
using Shared.Contracts.Dtos.v1.MembershipPackages;
using Shared.Contracts.Dtos.v1.Members;
using Shared.Contracts.Dtos.v1.Memberships;
using Shared.Contracts.Dtos.v1.Payments;
using Shared.Contracts.Dtos.v1.Staff;
using Shared.Contracts.Dtos.v1.System;
using Shared.Contracts.Dtos.v1.System.Platform;
using Shared.Contracts.Dtos.v1.TrainingCategories;
using Shared.Contracts.Dtos.v1.TrainingSessions;
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

    public static void AssertNoContent(IActionResult result)
    {
        Assert.IsType<NoContentResult>(result);
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

    public Func<string, Guid, MemberStatusUpdateRequest, CancellationToken, Task<MemberDetailResponse>> UpdateMemberStatusAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<MemberDetailResponse>(new InvalidOperationException("UpdateMemberStatusAsyncHandler not configured."));

    public Task<IReadOnlyCollection<MemberResponse>> GetMembersAsync(string gymCode, MemberFilter? filter = null, CancellationToken cancellationToken = default) =>
        GetMembersAsyncHandler(gymCode, cancellationToken);

    public Task<MemberDetailResponse> UpdateMemberStatusAsync(string gymCode, Guid id, MemberStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateMemberStatusAsyncHandler(gymCode, id, request, cancellationToken);

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
    public Func<string, CancellationToken, Task<IReadOnlyCollection<TrainingCategoryResponse>>> GetCategoriesAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<TrainingCategoryResponse>>([]);

    public Func<string, TrainingCategoryUpsertRequest, CancellationToken, Task<TrainingCategoryResponse>> CreateCategoryAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<TrainingCategoryResponse>(new InvalidOperationException("CreateCategoryAsyncHandler not configured."));

    public Func<string, Guid, TrainingCategoryUpsertRequest, CancellationToken, Task<TrainingCategoryResponse>> UpdateCategoryAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<TrainingCategoryResponse>(new InvalidOperationException("UpdateCategoryAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteCategoryAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<TrainingSessionResponse>>> GetSessionsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<TrainingSessionResponse>>([]);

    public Func<string, Guid, CancellationToken, Task<TrainingSessionResponse>> GetSessionAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<TrainingSessionResponse>(new InvalidOperationException("GetSessionAsyncHandler not configured."));

    public Func<string, Guid?, TrainingSessionUpsertRequest, CancellationToken, Task<TrainingSessionResponse>> UpsertTrainingSessionAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<TrainingSessionResponse>(new InvalidOperationException("UpsertTrainingSessionAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteSessionAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<BookingResponse>>> GetBookingsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<BookingResponse>>([]);

    public Func<string, BookingCreateRequest, CancellationToken, Task<BookingResponse>> CreateBookingAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<BookingResponse>(new InvalidOperationException("CreateBookingAsyncHandler not configured."));

    public Func<string, Guid, AttendanceUpdateRequest, CancellationToken, Task<BookingResponse>> UpdateAttendanceAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<BookingResponse>(new InvalidOperationException("UpdateAttendanceAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> CancelBookingAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, Guid, BookingRescheduleRequest, CancellationToken, Task<BookingResponse>> RescheduleBookingAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<BookingResponse>(new InvalidOperationException("RescheduleBookingAsyncHandler not configured."));

    public Func<string, Guid, TrainingSessionStatusUpdateRequest, CancellationToken, Task<TrainingSessionResponse>> UpdateSessionStatusAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<TrainingSessionResponse>(new InvalidOperationException("UpdateSessionStatusAsyncHandler not configured."));

    public Func<string, Guid, TrainingSessionTrainerUpdateRequest, CancellationToken, Task<TrainingSessionResponse>> UpdateSessionTrainerAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<TrainingSessionResponse>(new InvalidOperationException("UpdateSessionTrainerAsyncHandler not configured."));

    public Func<string, Guid, TrainingSessionRescheduleRequest, CancellationToken, Task<TrainingSessionResponse>> RescheduleSessionAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<TrainingSessionResponse>(new InvalidOperationException("RescheduleSessionAsyncHandler not configured."));

    public Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetCategoriesAsyncHandler(gymCode, cancellationToken);

    public Task<TrainingCategoryResponse> CreateCategoryAsync(string gymCode, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateCategoryAsyncHandler(gymCode, request, cancellationToken);

    public Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateCategoryAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteCategoryAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteCategoryAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode, TrainingSessionFilter? filter = null, CancellationToken cancellationToken = default) =>
        GetSessionsAsyncHandler(gymCode, cancellationToken);

    public Task<TrainingSessionResponse> GetSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        GetSessionAsyncHandler(gymCode, id, cancellationToken);

    public Task<TrainingSessionResponse> UpsertTrainingSessionAsync(string gymCode, Guid? sessionId, TrainingSessionUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpsertTrainingSessionAsyncHandler(gymCode, sessionId, request, cancellationToken);

    public Task<TrainingSessionResponse> UpdateSessionStatusAsync(string gymCode, Guid sessionId, TrainingSessionStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateSessionStatusAsyncHandler(gymCode, sessionId, request, cancellationToken);

    public Task<TrainingSessionResponse> UpdateSessionTrainerAsync(string gymCode, Guid sessionId, TrainingSessionTrainerUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateSessionTrainerAsyncHandler(gymCode, sessionId, request, cancellationToken);

    public Task<TrainingSessionResponse> RescheduleSessionAsync(string gymCode, Guid sessionId, TrainingSessionRescheduleRequest request, CancellationToken cancellationToken = default) =>
        RescheduleSessionAsyncHandler(gymCode, sessionId, request, cancellationToken);

    public Task DeleteSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteSessionAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<BookingResponse>> GetBookingsAsync(string gymCode, BookingFilter? filter = null, CancellationToken cancellationToken = default) =>
        GetBookingsAsyncHandler(gymCode, cancellationToken);

    public Task<BookingResponse> CreateBookingAsync(string gymCode, BookingCreateRequest request, CancellationToken cancellationToken = default) =>
        CreateBookingAsyncHandler(gymCode, request, cancellationToken);

    public Task<BookingResponse> UpdateAttendanceAsync(string gymCode, Guid bookingId, AttendanceUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateAttendanceAsyncHandler(gymCode, bookingId, request, cancellationToken);

    public Task<BookingResponse> RescheduleBookingAsync(string gymCode, Guid bookingId, BookingRescheduleRequest request, CancellationToken cancellationToken = default) =>
        RescheduleBookingAsyncHandler(gymCode, bookingId, request, cancellationToken);

    public Task CancelBookingAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        CancelBookingAsyncHandler(gymCode, id, cancellationToken);
}

public class DelegatingMembershipWorkflowService : IMembershipWorkflowService
{
    public Func<string, CancellationToken, Task<IReadOnlyCollection<MembershipPackageResponse>>> GetPackagesAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<MembershipPackageResponse>>([]);

    public Func<string, MembershipPackageUpsertRequest, CancellationToken, Task<MembershipPackageResponse>> CreatePackageAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<MembershipPackageResponse>(new InvalidOperationException("CreatePackageAsyncHandler not configured."));

    public Func<string, Guid, MembershipPackageUpsertRequest, CancellationToken, Task<MembershipPackageResponse>> UpdatePackageAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<MembershipPackageResponse>(new InvalidOperationException("UpdatePackageAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeletePackageAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<MembershipResponse>>> GetMembershipsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<MembershipResponse>>([]);

    public Func<string, SellMembershipRequest, CancellationToken, Task<MembershipSaleResponse>> SellMembershipAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<MembershipSaleResponse>(new InvalidOperationException("SellMembershipAsyncHandler not configured."));

    public Func<string, Guid, MembershipStatusUpdateRequest, CancellationToken, Task<MembershipResponse>> UpdateMembershipStatusAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<MembershipResponse>(new InvalidOperationException("UpdateMembershipStatusAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteMembershipAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<PaymentResponse>>> GetPaymentsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<PaymentResponse>>([]);

    public Func<string, PaymentCreateRequest, CancellationToken, Task<PaymentResponse>> CreatePaymentAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<PaymentResponse>(new InvalidOperationException("CreatePaymentAsyncHandler not configured."));

    public Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetPackagesAsyncHandler(gymCode, cancellationToken);

    public Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreatePackageAsyncHandler(gymCode, request, cancellationToken);

    public Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdatePackageAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeletePackageAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeletePackageAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode, MembershipFilter? filter = null, CancellationToken cancellationToken = default) =>
        GetMembershipsAsyncHandler(gymCode, cancellationToken);

    public Func<string, Guid, MembershipEditRequest, CancellationToken, Task<MembershipResponse>> UpdateMembershipAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<MembershipResponse>(new InvalidOperationException("UpdateMembershipAsyncHandler not configured."));

    public Task<MembershipResponse> UpdateMembershipAsync(string gymCode, Guid id, MembershipEditRequest request, CancellationToken cancellationToken = default) =>
        UpdateMembershipAsyncHandler(gymCode, id, request, cancellationToken);

    public Func<string, Guid, PaymentRefundRequest, CancellationToken, Task<PaymentResponse>> RefundPaymentAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<PaymentResponse>(new InvalidOperationException("RefundPaymentAsyncHandler not configured."));

    public Task<PaymentResponse> RefundPaymentAsync(string gymCode, Guid paymentId, PaymentRefundRequest request, CancellationToken cancellationToken = default) =>
        RefundPaymentAsyncHandler(gymCode, paymentId, request, cancellationToken);

    public Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request, CancellationToken cancellationToken = default) =>
        SellMembershipAsyncHandler(gymCode, request, cancellationToken);

    public Task<MembershipResponse> UpdateMembershipStatusAsync(string gymCode, Guid id, MembershipStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateMembershipStatusAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteMembershipAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, PaymentFilter? filter = null, CancellationToken cancellationToken = default) =>
        GetPaymentsAsyncHandler(gymCode, cancellationToken);

    public Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default) =>
        CreatePaymentAsyncHandler(gymCode, request, cancellationToken);
}

public class DelegatingMaintenanceWorkflowService : IMaintenanceWorkflowService
{
    public Func<string, CancellationToken, Task<IReadOnlyCollection<EquipmentModelResponse>>> GetEquipmentModelsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<EquipmentModelResponse>>([]);

    public Func<string, EquipmentModelUpsertRequest, CancellationToken, Task<EquipmentModelResponse>> CreateEquipmentModelAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<EquipmentModelResponse>(new InvalidOperationException("CreateEquipmentModelAsyncHandler not configured."));

    public Func<string, Guid, EquipmentModelUpsertRequest, CancellationToken, Task<EquipmentModelResponse>> UpdateEquipmentModelAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<EquipmentModelResponse>(new InvalidOperationException("UpdateEquipmentModelAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteEquipmentModelAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<EquipmentResponse>>> GetEquipmentAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<EquipmentResponse>>([]);

    public Func<string, EquipmentUpsertRequest, CancellationToken, Task<EquipmentResponse>> CreateEquipmentAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<EquipmentResponse>(new InvalidOperationException("CreateEquipmentAsyncHandler not configured."));

    public Func<string, Guid, EquipmentUpsertRequest, CancellationToken, Task<EquipmentResponse>> UpdateEquipmentAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<EquipmentResponse>(new InvalidOperationException("UpdateEquipmentAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteEquipmentAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<MaintenanceTaskResponse>>> GetMaintenanceTasksAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<MaintenanceTaskResponse>>([]);

    public Func<string, MaintenanceTaskUpsertRequest, CancellationToken, Task<MaintenanceTaskResponse>> CreateTaskAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<MaintenanceTaskResponse>(new InvalidOperationException("CreateTaskAsyncHandler not configured."));

    public Func<string, Guid, MaintenanceStatusUpdateRequest, CancellationToken, Task<MaintenanceTaskResponse>> UpdateTaskStatusAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<MaintenanceTaskResponse>(new InvalidOperationException("UpdateTaskStatusAsyncHandler not configured."));

    public Func<string, Guid, MaintenanceAssignmentUpdateRequest, CancellationToken, Task<MaintenanceTaskResponse>> UpdateTaskAssignmentAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<MaintenanceTaskResponse>(new InvalidOperationException("UpdateTaskAssignmentAsyncHandler not configured."));

    public Func<string, CancellationToken, Task<int>> GenerateDueScheduledTasksAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult(0);

    public Func<string, Guid, CancellationToken, Task> DeleteMaintenanceTaskAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<GymSettingsResponse>> GetGymSettingsAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<GymSettingsResponse>(new InvalidOperationException("GetGymSettingsAsyncHandler not configured."));

    public Func<string, GymSettingsUpdateRequest, CancellationToken, Task<GymSettingsResponse>> UpdateGymSettingsAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<GymSettingsResponse>(new InvalidOperationException("UpdateGymSettingsAsyncHandler not configured."));

    public Func<string, CancellationToken, Task<IReadOnlyCollection<GymUserResponse>>> GetGymUsersAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<GymUserResponse>>([]);

    public Func<string, GymUserUpsertRequest, CancellationToken, Task<GymUserResponse>> UpsertGymUserAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<GymUserResponse>(new InvalidOperationException("UpsertGymUserAsyncHandler not configured."));

    public Func<string, Guid, string, CancellationToken, Task> DeleteGymUserAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetEquipmentModelsAsyncHandler(gymCode, cancellationToken);

    public Task<EquipmentModelResponse> CreateEquipmentModelAsync(string gymCode, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateEquipmentModelAsyncHandler(gymCode, request, cancellationToken);

    public Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateEquipmentModelAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteEquipmentModelAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteEquipmentModelAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode, EquipmentFilter? filter = null, CancellationToken cancellationToken = default) =>
        GetEquipmentAsyncHandler(gymCode, cancellationToken);

    public Func<string, Guid, EquipmentStatusUpdateRequest, CancellationToken, Task<EquipmentResponse>> UpdateEquipmentStatusAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<EquipmentResponse>(new InvalidOperationException("UpdateEquipmentStatusAsyncHandler not configured."));

    public Task<EquipmentResponse> UpdateEquipmentStatusAsync(string gymCode, Guid id, EquipmentStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateEquipmentStatusAsyncHandler(gymCode, id, request, cancellationToken);

    public Task<EquipmentResponse> CreateEquipmentAsync(string gymCode, EquipmentUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateEquipmentAsyncHandler(gymCode, request, cancellationToken);

    public Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateEquipmentAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteEquipmentAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteEquipmentAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode, MaintenanceTaskFilter? filter = null, CancellationToken cancellationToken = default) =>
        GetMaintenanceTasksAsyncHandler(gymCode, cancellationToken);

    public Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateTaskAsyncHandler(gymCode, request, cancellationToken);

    public Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateTaskStatusAsyncHandler(gymCode, taskId, request, cancellationToken);

    public Task<MaintenanceTaskResponse> UpdateTaskAssignmentAsync(string gymCode, Guid taskId, MaintenanceAssignmentUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateTaskAssignmentAsyncHandler(gymCode, taskId, request, cancellationToken);

    public Task<int> GenerateDueScheduledTasksAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GenerateDueScheduledTasksAsyncHandler(gymCode, cancellationToken);

    public Task DeleteMaintenanceTaskAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteMaintenanceTaskAsyncHandler(gymCode, id, cancellationToken);

    public Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetGymSettingsAsyncHandler(gymCode, cancellationToken);

    public Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateGymSettingsAsyncHandler(gymCode, request, cancellationToken);

    public Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetGymUsersAsyncHandler(gymCode, cancellationToken);

    public Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpsertGymUserAsyncHandler(gymCode, request, cancellationToken);

    public Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName, CancellationToken cancellationToken = default) =>
        DeleteGymUserAsyncHandler(gymCode, appUserId, roleName, cancellationToken);
}

public sealed class DelegatingStaffWorkflowService : IStaffWorkflowService
{
    public Func<string, CancellationToken, Task<IReadOnlyCollection<StaffResponse>>> GetStaffAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<StaffResponse>>([]);

    public Func<string, StaffUpsertRequest, CancellationToken, Task<StaffResponse>> CreateStaffAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<StaffResponse>(new InvalidOperationException("CreateStaffAsyncHandler not configured."));

    public Func<string, Guid, StaffUpsertRequest, CancellationToken, Task<StaffResponse>> UpdateStaffAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<StaffResponse>(new InvalidOperationException("UpdateStaffAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteStaffAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<StaffResponse>> GetStaffAsync(string gymCode, StaffFilter? filter = null, CancellationToken cancellationToken = default) =>
        GetStaffAsyncHandler(gymCode, cancellationToken);

    public Func<string, Guid, StaffStatusUpdateRequest, CancellationToken, Task<StaffResponse>> UpdateStaffStatusAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<StaffResponse>(new InvalidOperationException("UpdateStaffStatusAsyncHandler not configured."));

    public Task<StaffResponse> UpdateStaffStatusAsync(string gymCode, Guid id, StaffStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateStaffStatusAsyncHandler(gymCode, id, request, cancellationToken);

    public Task<StaffResponse> CreateStaffAsync(string gymCode, StaffUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateStaffAsyncHandler(gymCode, request, cancellationToken);

    public Task<StaffResponse> UpdateStaffAsync(string gymCode, Guid id, StaffUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateStaffAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteStaffAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteStaffAsyncHandler(gymCode, id, cancellationToken);
}

public sealed class DelegatingPlatformService : IPlatformService
{
    public Func<CancellationToken, Task<IReadOnlyCollection<GymSummaryResponse>>> GetGymsAsyncHandler { get; set; } =
        static _ => Task.FromResult<IReadOnlyCollection<GymSummaryResponse>>([]);

    public Func<RegisterGymRequest, CancellationToken, Task<RegisterGymResponse>> RegisterGymAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<RegisterGymResponse>(new InvalidOperationException("RegisterGymAsyncHandler not configured."));

    public Func<Guid, UpdateGymActivationRequest, CancellationToken, Task> UpdateGymActivationAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<Guid, CancellationToken, Task<CompanySnapshotResponse>> GetGymSnapshotAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<CompanySnapshotResponse>(new InvalidOperationException("GetGymSnapshotAsyncHandler not configured."));

    public Func<CancellationToken, Task<PlatformAnalyticsResponse>> GetAnalyticsAsyncHandler { get; set; } =
        static _ => Task.FromException<PlatformAnalyticsResponse>(new InvalidOperationException("GetAnalyticsAsyncHandler not configured."));

    public Task<IReadOnlyCollection<GymSummaryResponse>> GetGymsAsync(CancellationToken cancellationToken = default) =>
        GetGymsAsyncHandler(cancellationToken);

    public Task<RegisterGymResponse> RegisterGymAsync(RegisterGymRequest request, CancellationToken cancellationToken = default) =>
        RegisterGymAsyncHandler(request, cancellationToken);

    public Task UpdateGymActivationAsync(Guid gymId, UpdateGymActivationRequest request, CancellationToken cancellationToken = default) =>
        UpdateGymActivationAsyncHandler(gymId, request, cancellationToken);

    public Task<CompanySnapshotResponse> GetGymSnapshotAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        GetGymSnapshotAsyncHandler(gymId, cancellationToken);

    public Task<PlatformAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken = default) =>
        GetAnalyticsAsyncHandler(cancellationToken);
}

public sealed class DelegatingIdentityService : IIdentityService
{
    public Func<RegisterRequest, CancellationToken, Task<JwtResponse>> RegisterAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<JwtResponse>(new InvalidOperationException("RegisterAsyncHandler not configured."));

    public Func<SwitchGymRequest, CancellationToken, Task<JwtResponse>> SwitchGymAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<JwtResponse>(new InvalidOperationException("SwitchGymAsyncHandler not configured."));

    public Func<SwitchRoleRequest, CancellationToken, Task<JwtResponse>> SwitchRoleAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<JwtResponse>(new InvalidOperationException("SwitchRoleAsyncHandler not configured."));

    public Func<ForgotPasswordRequest, CancellationToken, Task<ForgotPasswordResponse>> ForgotPasswordAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<ForgotPasswordResponse>(new InvalidOperationException("ForgotPasswordAsyncHandler not configured."));

    public Func<ResetPasswordRequest, CancellationToken, Task> ResetPasswordAsyncHandler { get; set; } =
        static (_, _) => Task.CompletedTask;

    public Task<JwtResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        RegisterAsyncHandler(request, cancellationToken);

    public Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request, CancellationToken cancellationToken = default) =>
        SwitchGymAsyncHandler(request, cancellationToken);

    public Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken cancellationToken = default) =>
        SwitchRoleAsyncHandler(request, cancellationToken);

    public Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default) =>
        ForgotPasswordAsyncHandler(request, cancellationToken);

    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
        ResetPasswordAsyncHandler(request, cancellationToken);
}

public sealed class DelegatingAccountAuthService : IAccountAuthService
{
    public Func<LoginRequest, CancellationToken, Task<JwtResponse>> LoginAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<JwtResponse>(new InvalidOperationException("LoginAsyncHandler not configured."));

    public Func<CancellationToken, Task> LogoutAsyncHandler { get; set; } =
        static _ => Task.CompletedTask;

    public Func<RefreshTokenRequest, CancellationToken, Task<JwtResponse>> RenewRefreshTokenAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<JwtResponse>(new InvalidOperationException("RenewRefreshTokenAsyncHandler not configured."));

    public Func<ChangePasswordRequest, CancellationToken, Task> ChangeOwnPasswordAsyncHandler { get; set; } =
        static (_, _) => Task.CompletedTask;

    public Task<JwtResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        LoginAsyncHandler(request, cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        LogoutAsyncHandler(cancellationToken);

    public Task<JwtResponse> RenewRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
        RenewRefreshTokenAsyncHandler(request, cancellationToken);

    public Task ChangeOwnPasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default) =>
        ChangeOwnPasswordAsyncHandler(request, cancellationToken);
}

public sealed class DelegatingMemberWorkspaceService : IMemberWorkspaceService
{
    public Func<string, CancellationToken, Task<MemberWorkspaceResponse>> GetCurrentWorkspaceAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<MemberWorkspaceResponse>(new InvalidOperationException("GetCurrentWorkspaceAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task<MemberWorkspaceResponse>> GetWorkspaceAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<MemberWorkspaceResponse>(new InvalidOperationException("GetWorkspaceAsyncHandler not configured."));

    public Task<MemberWorkspaceResponse> GetCurrentWorkspaceAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetCurrentWorkspaceAsyncHandler(gymCode, cancellationToken);

    public Task<MemberWorkspaceResponse> GetWorkspaceAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default) =>
        GetWorkspaceAsyncHandler(gymCode, memberId, cancellationToken);
}
