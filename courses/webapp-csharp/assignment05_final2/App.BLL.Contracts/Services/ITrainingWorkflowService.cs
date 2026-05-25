using System.Security.Claims;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using App.Domain.Identity;
using Shared.Contracts.Dtos.v1.Bookings;
using Shared.Contracts.Dtos.v1.TrainingCategories;
using Shared.Contracts.Dtos.v1.TrainingSessions;

namespace App.BLL.Contracts.Services;

public interface ITrainingWorkflowService
{
    Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<TrainingCategoryResponse> CreateCategoryAsync(string gymCode, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default);
    Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode, TrainingSessionFilter? filter = null, CancellationToken cancellationToken = default);
    Task<TrainingSessionResponse> GetSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<TrainingSessionResponse> UpsertTrainingSessionAsync(string gymCode, Guid? sessionId, TrainingSessionUpsertRequest request, CancellationToken cancellationToken = default);
    Task<TrainingSessionResponse> UpdateSessionStatusAsync(string gymCode, Guid sessionId, TrainingSessionStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<TrainingSessionResponse> UpdateSessionTrainerAsync(string gymCode, Guid sessionId, TrainingSessionTrainerUpdateRequest request, CancellationToken cancellationToken = default);
    Task<TrainingSessionResponse> RescheduleSessionAsync(string gymCode, Guid sessionId, TrainingSessionRescheduleRequest request, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BookingResponse>> GetBookingsAsync(string gymCode, BookingFilter? filter = null, CancellationToken cancellationToken = default);
    Task<BookingResponse> CreateBookingAsync(string gymCode, BookingCreateRequest request, CancellationToken cancellationToken = default);
    Task<BookingResponse> UpdateAttendanceAsync(string gymCode, Guid bookingId, AttendanceUpdateRequest request, CancellationToken cancellationToken = default);
    Task<BookingResponse> RescheduleBookingAsync(string gymCode, Guid bookingId, BookingRescheduleRequest request, CancellationToken cancellationToken = default);
    Task CancelBookingAsync(string gymCode, Guid id, CancellationToken cancellationToken = default);
}
