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
    Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode);
    Task<TrainingCategoryResponse> CreateCategoryAsync(string gymCode, TrainingCategoryUpsertRequest request);
    Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request);
    Task DeleteCategoryAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode);
    Task<TrainingSessionResponse> GetSessionAsync(string gymCode, Guid id);
    Task<TrainingSessionResponse> UpsertTrainingSessionAsync(string gymCode, Guid? sessionId, TrainingSessionUpsertRequest request);
    Task DeleteSessionAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<WorkShiftResponse>> GetWorkShiftsAsync(string gymCode);
    Task<WorkShiftResponse> CreateWorkShiftAsync(string gymCode, WorkShiftUpsertRequest request);
    Task<WorkShiftResponse> UpdateWorkShiftAsync(string gymCode, Guid id, WorkShiftUpsertRequest request);
    Task DeleteWorkShiftAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<BookingResponse>> GetBookingsAsync(string gymCode);
    Task<BookingResponse> CreateBookingAsync(string gymCode, BookingCreateRequest request);
    Task<BookingResponse> UpdateAttendanceAsync(string gymCode, Guid bookingId, AttendanceUpdateRequest request);
    Task CancelBookingAsync(string gymCode, Guid id);
}
