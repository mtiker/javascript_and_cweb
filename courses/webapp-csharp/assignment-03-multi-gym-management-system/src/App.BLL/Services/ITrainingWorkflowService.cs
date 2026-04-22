using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;
using App.DTO.v1.WorkShifts;

namespace App.BLL.Services;

public interface ITrainingWorkflowService
{
    Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<TrainingCategoryResponse> CreateCategoryAsync(string gymCode, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default);
    Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<TrainingSessionResponse> GetSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<TrainingSessionResponse> UpsertTrainingSessionAsync(string gymCode, Guid? sessionId, TrainingSessionUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkShiftResponse>> GetWorkShiftsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<WorkShiftResponse> CreateWorkShiftAsync(string gymCode, WorkShiftUpsertRequest request, CancellationToken cancellationToken = default);
    Task<WorkShiftResponse> UpdateWorkShiftAsync(string gymCode, Guid id, WorkShiftUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteWorkShiftAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BookingResponse>> GetBookingsAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<BookingResponse> CreateBookingAsync(string gymCode, BookingCreateRequest request, CancellationToken cancellationToken = default);
    Task<BookingResponse> UpdateAttendanceAsync(string gymCode, Guid bookingId, AttendanceUpdateRequest request, CancellationToken cancellationToken = default);
    Task CancelBookingAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
