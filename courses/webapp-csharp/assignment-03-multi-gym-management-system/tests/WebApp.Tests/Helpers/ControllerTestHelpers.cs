using System.Security.Claims;
using App.BLL.Services;
using App.Domain.Entities;
using App.DTO.v1;
using App.DTO.v1.Bookings;
using App.DTO.v1.CoachingPlans;
using App.DTO.v1.EmploymentContracts;
using App.DTO.v1.Equipment;
using App.DTO.v1.EquipmentModels;
using App.DTO.v1.Finance;
using App.DTO.v1.GymSettings;
using App.DTO.v1.GymUsers;
using App.DTO.v1.Identity;
using App.DTO.v1.JobRoles;
using App.DTO.v1.MaintenanceTasks;
using App.DTO.v1.MemberWorkspace;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Members;
using App.DTO.v1.Memberships;
using App.DTO.v1.OpeningHours;
using App.DTO.v1.OpeningHoursExceptions;
using App.DTO.v1.Payments;
using App.DTO.v1.Staff;
using App.DTO.v1.System;
using App.DTO.v1.System.Billing;
using App.DTO.v1.System.Platform;
using App.DTO.v1.System.Support;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;
using App.DTO.v1.Vacations;
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

    public Func<string, CancellationToken, Task<IReadOnlyCollection<WorkShiftResponse>>> GetWorkShiftsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<WorkShiftResponse>>([]);

    public Func<string, WorkShiftUpsertRequest, CancellationToken, Task<WorkShiftResponse>> CreateWorkShiftAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<WorkShiftResponse>(new InvalidOperationException("CreateWorkShiftAsyncHandler not configured."));

    public Func<string, Guid, WorkShiftUpsertRequest, CancellationToken, Task<WorkShiftResponse>> UpdateWorkShiftAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<WorkShiftResponse>(new InvalidOperationException("UpdateWorkShiftAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteWorkShiftAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<BookingResponse>>> GetBookingsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<BookingResponse>>([]);

    public Func<string, BookingCreateRequest, CancellationToken, Task<BookingResponse>> CreateBookingAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<BookingResponse>(new InvalidOperationException("CreateBookingAsyncHandler not configured."));

    public Func<string, Guid, AttendanceUpdateRequest, CancellationToken, Task<BookingResponse>> UpdateAttendanceAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<BookingResponse>(new InvalidOperationException("UpdateAttendanceAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> CancelBookingAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetCategoriesAsyncHandler(gymCode, cancellationToken);

    public Task<TrainingCategoryResponse> CreateCategoryAsync(string gymCode, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateCategoryAsyncHandler(gymCode, request, cancellationToken);

    public Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateCategoryAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteCategoryAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteCategoryAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetSessionsAsyncHandler(gymCode, cancellationToken);

    public Task<TrainingSessionResponse> GetSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        GetSessionAsyncHandler(gymCode, id, cancellationToken);

    public Task<TrainingSessionResponse> UpsertTrainingSessionAsync(string gymCode, Guid? sessionId, TrainingSessionUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpsertTrainingSessionAsyncHandler(gymCode, sessionId, request, cancellationToken);

    public Task DeleteSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteSessionAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<WorkShiftResponse>> GetWorkShiftsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetWorkShiftsAsyncHandler(gymCode, cancellationToken);

    public Task<WorkShiftResponse> CreateWorkShiftAsync(string gymCode, WorkShiftUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateWorkShiftAsyncHandler(gymCode, request, cancellationToken);

    public Task<WorkShiftResponse> UpdateWorkShiftAsync(string gymCode, Guid id, WorkShiftUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateWorkShiftAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteWorkShiftAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteWorkShiftAsyncHandler(gymCode, id, cancellationToken);

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

    public Func<string, Guid, MembershipStatusUpdateRequest, CancellationToken, Task<MembershipResponse>> UpdateMembershipStatusAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<MembershipResponse>(new InvalidOperationException("UpdateMembershipStatusAsyncHandler not configured."));

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

    public Task<MembershipResponse> UpdateMembershipStatusAsync(string gymCode, Guid id, MembershipStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateMembershipStatusAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteMembershipAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteMembershipAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<PaymentResponse>>([]);

    public Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<PaymentResponse>(new InvalidOperationException("CreatePaymentAsync not configured."));

    public Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession, CancellationToken cancellationToken = default) =>
        Task.FromResult(0m);
}

public sealed class DelegatingMaintenanceWorkflowService : IMaintenanceWorkflowService
{
    public Func<string, CancellationToken, Task<IReadOnlyCollection<OpeningHoursResponse>>> GetOpeningHoursAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<OpeningHoursResponse>>([]);

    public Func<string, OpeningHoursUpsertRequest, CancellationToken, Task<OpeningHoursResponse>> CreateOpeningHoursAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<OpeningHoursResponse>(new InvalidOperationException("CreateOpeningHoursAsyncHandler not configured."));

    public Func<string, Guid, OpeningHoursUpsertRequest, CancellationToken, Task<OpeningHoursResponse>> UpdateOpeningHoursAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<OpeningHoursResponse>(new InvalidOperationException("UpdateOpeningHoursAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteOpeningHoursAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<OpeningHoursExceptionResponse>>> GetOpeningHourExceptionsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<OpeningHoursExceptionResponse>>([]);

    public Func<string, OpeningHoursExceptionUpsertRequest, CancellationToken, Task<OpeningHoursExceptionResponse>> CreateOpeningHourExceptionAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<OpeningHoursExceptionResponse>(new InvalidOperationException("CreateOpeningHourExceptionAsyncHandler not configured."));

    public Func<string, Guid, OpeningHoursExceptionUpsertRequest, CancellationToken, Task<OpeningHoursExceptionResponse>> UpdateOpeningHourExceptionAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<OpeningHoursExceptionResponse>(new InvalidOperationException("UpdateOpeningHourExceptionAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteOpeningHourExceptionAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

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

    public Func<string, Guid, CancellationToken, Task<IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>>> GetTaskAssignmentHistoryAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromResult<IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>>([]);

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

    public Task<IReadOnlyCollection<OpeningHoursResponse>> GetOpeningHoursAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetOpeningHoursAsyncHandler(gymCode, cancellationToken);

    public Task<OpeningHoursResponse> CreateOpeningHoursAsync(string gymCode, OpeningHoursUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateOpeningHoursAsyncHandler(gymCode, request, cancellationToken);

    public Task<OpeningHoursResponse> UpdateOpeningHoursAsync(string gymCode, Guid id, OpeningHoursUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateOpeningHoursAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteOpeningHoursAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteOpeningHoursAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<OpeningHoursExceptionResponse>> GetOpeningHourExceptionsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetOpeningHourExceptionsAsyncHandler(gymCode, cancellationToken);

    public Task<OpeningHoursExceptionResponse> CreateOpeningHourExceptionAsync(string gymCode, OpeningHoursExceptionUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateOpeningHourExceptionAsyncHandler(gymCode, request, cancellationToken);

    public Task<OpeningHoursExceptionResponse> UpdateOpeningHourExceptionAsync(string gymCode, Guid id, OpeningHoursExceptionUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateOpeningHourExceptionAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteOpeningHourExceptionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteOpeningHourExceptionAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetEquipmentModelsAsyncHandler(gymCode, cancellationToken);

    public Task<EquipmentModelResponse> CreateEquipmentModelAsync(string gymCode, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateEquipmentModelAsyncHandler(gymCode, request, cancellationToken);

    public Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateEquipmentModelAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteEquipmentModelAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteEquipmentModelAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetEquipmentAsyncHandler(gymCode, cancellationToken);

    public Task<EquipmentResponse> CreateEquipmentAsync(string gymCode, EquipmentUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateEquipmentAsyncHandler(gymCode, request, cancellationToken);

    public Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateEquipmentAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteEquipmentAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteEquipmentAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetMaintenanceTasksAsyncHandler(gymCode, cancellationToken);

    public Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateTaskAsyncHandler(gymCode, request, cancellationToken);

    public Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateTaskStatusAsyncHandler(gymCode, taskId, request, cancellationToken);

    public Task<MaintenanceTaskResponse> UpdateTaskAssignmentAsync(string gymCode, Guid taskId, MaintenanceAssignmentUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdateTaskAssignmentAsyncHandler(gymCode, taskId, request, cancellationToken);

    public Task<IReadOnlyCollection<MaintenanceTaskAssignmentHistoryResponse>> GetTaskAssignmentHistoryAsync(string gymCode, Guid taskId, CancellationToken cancellationToken = default) =>
        GetTaskAssignmentHistoryAsyncHandler(gymCode, taskId, cancellationToken);

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

    public Func<string, CancellationToken, Task<IReadOnlyCollection<JobRoleResponse>>> GetJobRolesAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<JobRoleResponse>>([]);

    public Func<string, JobRoleUpsertRequest, CancellationToken, Task<JobRoleResponse>> CreateJobRoleAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<JobRoleResponse>(new InvalidOperationException("CreateJobRoleAsyncHandler not configured."));

    public Func<string, Guid, JobRoleUpsertRequest, CancellationToken, Task<JobRoleResponse>> UpdateJobRoleAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<JobRoleResponse>(new InvalidOperationException("UpdateJobRoleAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteJobRoleAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<ContractResponse>>> GetContractsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<ContractResponse>>([]);

    public Func<string, ContractUpsertRequest, CancellationToken, Task<ContractResponse>> CreateContractAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<ContractResponse>(new InvalidOperationException("CreateContractAsyncHandler not configured."));

    public Func<string, Guid, ContractUpsertRequest, CancellationToken, Task<ContractResponse>> UpdateContractAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<ContractResponse>(new InvalidOperationException("UpdateContractAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteContractAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<string, CancellationToken, Task<IReadOnlyCollection<VacationResponse>>> GetVacationsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<VacationResponse>>([]);

    public Func<string, VacationUpsertRequest, CancellationToken, Task<VacationResponse>> CreateVacationAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<VacationResponse>(new InvalidOperationException("CreateVacationAsyncHandler not configured."));

    public Func<string, Guid, VacationUpsertRequest, CancellationToken, Task<VacationResponse>> UpdateVacationAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<VacationResponse>(new InvalidOperationException("UpdateVacationAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeleteVacationAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<StaffResponse>> GetStaffAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetStaffAsyncHandler(gymCode, cancellationToken);

    public Task<StaffResponse> CreateStaffAsync(string gymCode, StaffUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateStaffAsyncHandler(gymCode, request, cancellationToken);

    public Task<StaffResponse> UpdateStaffAsync(string gymCode, Guid id, StaffUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateStaffAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteStaffAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteStaffAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<JobRoleResponse>> GetJobRolesAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetJobRolesAsyncHandler(gymCode, cancellationToken);

    public Task<JobRoleResponse> CreateJobRoleAsync(string gymCode, JobRoleUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateJobRoleAsyncHandler(gymCode, request, cancellationToken);

    public Task<JobRoleResponse> UpdateJobRoleAsync(string gymCode, Guid id, JobRoleUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateJobRoleAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteJobRoleAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteJobRoleAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<ContractResponse>> GetContractsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetContractsAsyncHandler(gymCode, cancellationToken);

    public Task<ContractResponse> CreateContractAsync(string gymCode, ContractUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateContractAsyncHandler(gymCode, request, cancellationToken);

    public Task<ContractResponse> UpdateContractAsync(string gymCode, Guid id, ContractUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateContractAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteContractAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteContractAsyncHandler(gymCode, id, cancellationToken);

    public Task<IReadOnlyCollection<VacationResponse>> GetVacationsAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetVacationsAsyncHandler(gymCode, cancellationToken);

    public Task<VacationResponse> CreateVacationAsync(string gymCode, VacationUpsertRequest request, CancellationToken cancellationToken = default) =>
        CreateVacationAsyncHandler(gymCode, request, cancellationToken);

    public Task<VacationResponse> UpdateVacationAsync(string gymCode, Guid id, VacationUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpdateVacationAsyncHandler(gymCode, id, request, cancellationToken);

    public Task DeleteVacationAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeleteVacationAsyncHandler(gymCode, id, cancellationToken);
}

public sealed class DelegatingPlatformService : IPlatformService
{
    public Func<CancellationToken, Task<IReadOnlyCollection<GymSummaryResponse>>> GetGymsAsyncHandler { get; set; } =
        static _ => Task.FromResult<IReadOnlyCollection<GymSummaryResponse>>([]);

    public Func<RegisterGymRequest, CancellationToken, Task<RegisterGymResponse>> RegisterGymAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<RegisterGymResponse>(new InvalidOperationException("RegisterGymAsyncHandler not configured."));

    public Func<Guid, UpdateGymActivationRequest, CancellationToken, Task> UpdateGymActivationAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<CancellationToken, Task<IReadOnlyCollection<SubscriptionSummaryResponse>>> GetSubscriptionsAsyncHandler { get; set; } =
        static _ => Task.FromResult<IReadOnlyCollection<SubscriptionSummaryResponse>>([]);

    public Func<Guid, UpdateSubscriptionRequest, CancellationToken, Task<SubscriptionSummaryResponse>> UpdateSubscriptionAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<SubscriptionSummaryResponse>(new InvalidOperationException("UpdateSubscriptionAsyncHandler not configured."));

    public Func<CancellationToken, Task<IReadOnlyCollection<SupportTicketResponse>>> GetSupportTicketsAsyncHandler { get; set; } =
        static _ => Task.FromResult<IReadOnlyCollection<SupportTicketResponse>>([]);

    public Func<Guid, SupportTicketRequest, CancellationToken, Task<SupportTicketResponse>> CreateSupportTicketAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<SupportTicketResponse>(new InvalidOperationException("CreateSupportTicketAsyncHandler not configured."));

    public Func<Guid, CancellationToken, Task<CompanySnapshotResponse>> GetGymSnapshotAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<CompanySnapshotResponse>(new InvalidOperationException("GetGymSnapshotAsyncHandler not configured."));

    public Func<CancellationToken, Task<PlatformAnalyticsResponse>> GetAnalyticsAsyncHandler { get; set; } =
        static _ => Task.FromException<PlatformAnalyticsResponse>(new InvalidOperationException("GetAnalyticsAsyncHandler not configured."));

    public Func<StartImpersonationRequest, CancellationToken, Task<StartImpersonationResponse>> StartImpersonationAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<StartImpersonationResponse>(new InvalidOperationException("StartImpersonationAsyncHandler not configured."));

    public Task<IReadOnlyCollection<GymSummaryResponse>> GetGymsAsync(CancellationToken cancellationToken = default) =>
        GetGymsAsyncHandler(cancellationToken);

    public Task<RegisterGymResponse> RegisterGymAsync(RegisterGymRequest request, CancellationToken cancellationToken = default) =>
        RegisterGymAsyncHandler(request, cancellationToken);

    public Task UpdateGymActivationAsync(Guid gymId, UpdateGymActivationRequest request, CancellationToken cancellationToken = default) =>
        UpdateGymActivationAsyncHandler(gymId, request, cancellationToken);

    public Task<IReadOnlyCollection<SubscriptionSummaryResponse>> GetSubscriptionsAsync(CancellationToken cancellationToken = default) =>
        GetSubscriptionsAsyncHandler(cancellationToken);

    public Task<SubscriptionSummaryResponse> UpdateSubscriptionAsync(Guid gymId, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default) =>
        UpdateSubscriptionAsyncHandler(gymId, request, cancellationToken);

    public Task<IReadOnlyCollection<SupportTicketResponse>> GetSupportTicketsAsync(CancellationToken cancellationToken = default) =>
        GetSupportTicketsAsyncHandler(cancellationToken);

    public Task<SupportTicketResponse> CreateSupportTicketAsync(Guid gymId, SupportTicketRequest request, CancellationToken cancellationToken = default) =>
        CreateSupportTicketAsyncHandler(gymId, request, cancellationToken);

    public Task<CompanySnapshotResponse> GetGymSnapshotAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        GetGymSnapshotAsyncHandler(gymId, cancellationToken);

    public Task<PlatformAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken = default) =>
        GetAnalyticsAsyncHandler(cancellationToken);

    public Task<StartImpersonationResponse> StartImpersonationAsync(StartImpersonationRequest request, CancellationToken cancellationToken = default) =>
        StartImpersonationAsyncHandler(request, cancellationToken);
}

public sealed class DelegatingIdentityService : IIdentityService
{
    public Func<RegisterRequest, CancellationToken, Task<JwtResponse>> RegisterAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<JwtResponse>(new InvalidOperationException("RegisterAsyncHandler not configured."));

    public Func<LoginRequest, CancellationToken, Task<JwtResponse>> LoginAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<JwtResponse>(new InvalidOperationException("LoginAsyncHandler not configured."));

    public Func<CancellationToken, Task> LogoutAsyncHandler { get; set; } =
        static _ => Task.CompletedTask;

    public Func<RefreshTokenRequest, CancellationToken, Task<JwtResponse>> RenewRefreshTokenAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<JwtResponse>(new InvalidOperationException("RenewRefreshTokenAsyncHandler not configured."));

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

    public Task<JwtResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        LoginAsyncHandler(request, cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        LogoutAsyncHandler(cancellationToken);

    public Task<JwtResponse> RenewRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
        RenewRefreshTokenAsyncHandler(request, cancellationToken);

    public Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request, CancellationToken cancellationToken = default) =>
        SwitchGymAsyncHandler(request, cancellationToken);

    public Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request, CancellationToken cancellationToken = default) =>
        SwitchRoleAsyncHandler(request, cancellationToken);

    public Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default) =>
        ForgotPasswordAsyncHandler(request, cancellationToken);

    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
        ResetPasswordAsyncHandler(request, cancellationToken);
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

public sealed class DelegatingCoachingPlanService : ICoachingPlanService
{
    public Func<string, Guid?, CancellationToken, Task<IReadOnlyCollection<CoachingPlanResponse>>> GetPlansAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromResult<IReadOnlyCollection<CoachingPlanResponse>>([]);

    public Func<string, Guid, CancellationToken, Task<CoachingPlanResponse>> GetPlanAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<CoachingPlanResponse>(new InvalidOperationException("GetPlanAsyncHandler not configured."));

    public Func<string, CoachingPlanCreateRequest, CancellationToken, Task<CoachingPlanResponse>> CreatePlanAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<CoachingPlanResponse>(new InvalidOperationException("CreatePlanAsyncHandler not configured."));

    public Func<string, Guid, CoachingPlanUpdateRequest, CancellationToken, Task<CoachingPlanResponse>> UpdatePlanAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<CoachingPlanResponse>(new InvalidOperationException("UpdatePlanAsyncHandler not configured."));

    public Func<string, Guid, CoachingPlanStatusUpdateRequest, CancellationToken, Task<CoachingPlanResponse>> UpdatePlanStatusAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<CoachingPlanResponse>(new InvalidOperationException("UpdatePlanStatusAsyncHandler not configured."));

    public Func<string, Guid, Guid, CoachingPlanItemDecisionRequest, CancellationToken, Task<CoachingPlanResponse>> DecidePlanItemAsyncHandler { get; set; } =
        static (_, _, _, _, _) => Task.FromException<CoachingPlanResponse>(new InvalidOperationException("DecidePlanItemAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task> DeletePlanAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<CoachingPlanResponse>> GetPlansAsync(string gymCode, Guid? memberId, CancellationToken cancellationToken = default) =>
        GetPlansAsyncHandler(gymCode, memberId, cancellationToken);

    public Task<CoachingPlanResponse> GetPlanAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        GetPlanAsyncHandler(gymCode, id, cancellationToken);

    public Task<CoachingPlanResponse> CreatePlanAsync(string gymCode, CoachingPlanCreateRequest request, CancellationToken cancellationToken = default) =>
        CreatePlanAsyncHandler(gymCode, request, cancellationToken);

    public Task<CoachingPlanResponse> UpdatePlanAsync(string gymCode, Guid id, CoachingPlanUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdatePlanAsyncHandler(gymCode, id, request, cancellationToken);

    public Task<CoachingPlanResponse> UpdatePlanStatusAsync(string gymCode, Guid id, CoachingPlanStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        UpdatePlanStatusAsyncHandler(gymCode, id, request, cancellationToken);

    public Task<CoachingPlanResponse> DecidePlanItemAsync(string gymCode, Guid id, Guid itemId, CoachingPlanItemDecisionRequest request, CancellationToken cancellationToken = default) =>
        DecidePlanItemAsyncHandler(gymCode, id, itemId, request, cancellationToken);

    public Task DeletePlanAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        DeletePlanAsyncHandler(gymCode, id, cancellationToken);
}

public sealed class DelegatingFinanceWorkspaceService : IFinanceWorkspaceService
{
    public Func<string, CancellationToken, Task<FinanceWorkspaceResponse>> GetCurrentWorkspaceAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<FinanceWorkspaceResponse>(new InvalidOperationException("GetCurrentWorkspaceAsyncHandler not configured."));

    public Func<string, Guid, CancellationToken, Task<FinanceWorkspaceResponse>> GetWorkspaceAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<FinanceWorkspaceResponse>(new InvalidOperationException("GetWorkspaceAsyncHandler not configured."));

    public Func<string, Guid?, CancellationToken, Task<IReadOnlyCollection<InvoiceResponse>>> GetInvoicesAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromResult<IReadOnlyCollection<InvoiceResponse>>([]);

    public Func<string, Guid, CancellationToken, Task<InvoiceResponse>> GetInvoiceAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<InvoiceResponse>(new InvalidOperationException("GetInvoiceAsyncHandler not configured."));

    public Func<string, InvoiceCreateRequest, CancellationToken, Task<InvoiceResponse>> CreateInvoiceAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<InvoiceResponse>(new InvalidOperationException("CreateInvoiceAsyncHandler not configured."));

    public Func<string, Guid, InvoicePaymentRequest, CancellationToken, Task<InvoiceResponse>> AddInvoicePaymentAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<InvoiceResponse>(new InvalidOperationException("AddInvoicePaymentAsyncHandler not configured."));

    public Func<string, Guid, InvoicePaymentRequest, CancellationToken, Task<InvoiceResponse>> AddInvoiceRefundAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<InvoiceResponse>(new InvalidOperationException("AddInvoiceRefundAsyncHandler not configured."));

    public Task<FinanceWorkspaceResponse> GetCurrentWorkspaceAsync(string gymCode, CancellationToken cancellationToken = default) =>
        GetCurrentWorkspaceAsyncHandler(gymCode, cancellationToken);

    public Task<FinanceWorkspaceResponse> GetWorkspaceAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default) =>
        GetWorkspaceAsyncHandler(gymCode, memberId, cancellationToken);

    public Task<IReadOnlyCollection<InvoiceResponse>> GetInvoicesAsync(string gymCode, Guid? memberId, CancellationToken cancellationToken = default) =>
        GetInvoicesAsyncHandler(gymCode, memberId, cancellationToken);

    public Task<InvoiceResponse> GetInvoiceAsync(string gymCode, Guid id, CancellationToken cancellationToken = default) =>
        GetInvoiceAsyncHandler(gymCode, id, cancellationToken);

    public Task<InvoiceResponse> CreateInvoiceAsync(string gymCode, InvoiceCreateRequest request, CancellationToken cancellationToken = default) =>
        CreateInvoiceAsyncHandler(gymCode, request, cancellationToken);

    public Task<InvoiceResponse> AddInvoicePaymentAsync(string gymCode, Guid id, InvoicePaymentRequest request, CancellationToken cancellationToken = default) =>
        AddInvoicePaymentAsyncHandler(gymCode, id, request, cancellationToken);

    public Task<InvoiceResponse> AddInvoiceRefundAsync(string gymCode, Guid id, InvoicePaymentRequest request, CancellationToken cancellationToken = default) =>
        AddInvoiceRefundAsyncHandler(gymCode, id, request, cancellationToken);
}
