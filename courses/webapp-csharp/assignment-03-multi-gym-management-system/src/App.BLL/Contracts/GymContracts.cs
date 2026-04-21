using System.Security.Claims;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using App.DTO.v1.Identity;
using App.DTO.v1.System;
using App.DTO.v1.Tenant;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Contracts;

public sealed record UserExecutionContext(
    Guid? UserId,
    Guid? PersonId,
    Guid? ActiveGymId,
    string? ActiveGymCode,
    string? ActiveRole,
    IReadOnlyCollection<string> AllRoles,
    IReadOnlyCollection<string> SystemRoles)
{
    public bool IsAuthenticated => UserId.HasValue;
    public bool HasRole(string roleName) => AllRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    public bool HasAnyRole(params IEnumerable<string> roleNames) => roleNames.Any(HasRole);
}

public interface IUserContextService
{
    UserExecutionContext GetCurrent();
}

public interface IAppDbContext
{
    DbSet<AppRefreshToken> RefreshTokens { get; }
    DbSet<Gym> Gyms { get; }
    DbSet<GymSettings> GymSettings { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<SupportTicket> SupportTickets { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AppUserGymRole> AppUserGymRoles { get; }
    DbSet<AppUser> Users { get; }
    DbSet<Person> People { get; }
    DbSet<Contact> Contacts { get; }
    DbSet<PersonContact> PersonContacts { get; }
    DbSet<GymContact> GymContacts { get; }
    DbSet<Member> Members { get; }
    DbSet<Staff> Staff { get; }
    DbSet<JobRole> JobRoles { get; }
    DbSet<EmploymentContract> EmploymentContracts { get; }
    DbSet<Vacation> Vacations { get; }
    DbSet<TrainingCategory> TrainingCategories { get; }
    DbSet<TrainingSession> TrainingSessions { get; }
    DbSet<WorkShift> WorkShifts { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<MembershipPackage> MembershipPackages { get; }
    DbSet<Membership> Memberships { get; }
    DbSet<Payment> Payments { get; }
    DbSet<OpeningHours> OpeningHours { get; }
    DbSet<OpeningHoursException> OpeningHoursExceptions { get; }
    DbSet<EquipmentModel> EquipmentModels { get; }
    DbSet<Equipment> Equipment { get; }
    DbSet<MaintenanceTask> MaintenanceTasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ITokenService
{
    string CreateJwt(AppUser user, IReadOnlyCollection<string> systemRoles, AppUserGymRole? activeGymRole);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string jwt);
    AppRefreshToken CreateRefreshToken(Guid userId, AppRefreshToken? previousToken = null);
    int AccessTokenLifetimeSeconds { get; }
}

public interface IIdentityService
{
    Task<JwtResponse> RegisterAsync(RegisterRequest request);
    Task<JwtResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<JwtResponse> RenewRefreshTokenAsync(RefreshTokenRequest request);
    Task<JwtResponse> SwitchGymAsync(SwitchGymRequest request);
    Task<JwtResponse> SwitchRoleAsync(SwitchRoleRequest request);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}

public interface IPlatformService
{
    Task<IReadOnlyCollection<GymSummaryResponse>> GetGymsAsync();
    Task<RegisterGymResponse> RegisterGymAsync(RegisterGymRequest request);
    Task UpdateGymActivationAsync(Guid gymId, UpdateGymActivationRequest request);
    Task<IReadOnlyCollection<SubscriptionSummaryResponse>> GetSubscriptionsAsync();
    Task<SubscriptionSummaryResponse> UpdateSubscriptionAsync(Guid gymId, UpdateSubscriptionRequest request);
    Task<IReadOnlyCollection<SupportTicketResponse>> GetSupportTicketsAsync();
    Task<SupportTicketResponse> CreateSupportTicketAsync(Guid gymId, SupportTicketRequest request);
    Task<CompanySnapshotResponse> GetGymSnapshotAsync(Guid gymId);
    Task<PlatformAnalyticsResponse> GetAnalyticsAsync();
    Task<StartImpersonationResponse> StartImpersonationAsync(StartImpersonationRequest request);
}

public interface IAuthorizationService
{
    Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles);
    Task<Member?> GetCurrentMemberAsync(Guid gymId);
    Task<Staff?> GetCurrentStaffAsync(Guid gymId);
    Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId);
    Task EnsureBookingAccessAsync(Booking booking);
    Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession);
    Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task);
}

public interface IMemberWorkflowService
{
    Task<IReadOnlyCollection<MemberResponse>> GetMembersAsync(string gymCode);
    Task<MemberDetailResponse> GetCurrentMemberAsync(string gymCode);
    Task<MemberDetailResponse> GetMemberAsync(string gymCode, Guid id);
    Task<MemberDetailResponse> CreateMemberAsync(string gymCode, MemberUpsertRequest request);
    Task<MemberDetailResponse> UpdateMemberAsync(string gymCode, Guid id, MemberUpsertRequest request);
    Task DeleteMemberAsync(string gymCode, Guid id);
}

public interface IMembershipWorkflowService
{
    Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode);
    Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request);
    Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request);
    Task DeletePackageAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<MembershipResponse>> GetMembershipsAsync(string gymCode);
    Task<MembershipSaleResponse> SellMembershipAsync(string gymCode, SellMembershipRequest request);
    Task DeleteMembershipAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<PaymentResponse>> GetPaymentsAsync(string gymCode);
    Task<PaymentResponse> CreatePaymentAsync(string gymCode, PaymentCreateRequest request);
    Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession);
}

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

public interface IMaintenanceWorkflowService
{
    Task<IReadOnlyCollection<OpeningHoursResponse>> GetOpeningHoursAsync(string gymCode);
    Task<OpeningHoursResponse> CreateOpeningHoursAsync(string gymCode, OpeningHoursUpsertRequest request);
    Task<OpeningHoursResponse> UpdateOpeningHoursAsync(string gymCode, Guid id, OpeningHoursUpsertRequest request);
    Task DeleteOpeningHoursAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<OpeningHoursExceptionResponse>> GetOpeningHourExceptionsAsync(string gymCode);
    Task<OpeningHoursExceptionResponse> CreateOpeningHourExceptionAsync(string gymCode, OpeningHoursExceptionUpsertRequest request);
    Task<OpeningHoursExceptionResponse> UpdateOpeningHourExceptionAsync(string gymCode, Guid id, OpeningHoursExceptionUpsertRequest request);
    Task DeleteOpeningHourExceptionAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<EquipmentModelResponse>> GetEquipmentModelsAsync(string gymCode);
    Task<EquipmentModelResponse> CreateEquipmentModelAsync(string gymCode, EquipmentModelUpsertRequest request);
    Task<EquipmentModelResponse> UpdateEquipmentModelAsync(string gymCode, Guid id, EquipmentModelUpsertRequest request);
    Task DeleteEquipmentModelAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<EquipmentResponse>> GetEquipmentAsync(string gymCode);
    Task<EquipmentResponse> CreateEquipmentAsync(string gymCode, EquipmentUpsertRequest request);
    Task<EquipmentResponse> UpdateEquipmentAsync(string gymCode, Guid id, EquipmentUpsertRequest request);
    Task DeleteEquipmentAsync(string gymCode, Guid id);
    Task<IReadOnlyCollection<MaintenanceTaskResponse>> GetMaintenanceTasksAsync(string gymCode);
    Task<MaintenanceTaskResponse> CreateTaskAsync(string gymCode, MaintenanceTaskUpsertRequest request);
    Task<MaintenanceTaskResponse> UpdateTaskStatusAsync(string gymCode, Guid taskId, MaintenanceStatusUpdateRequest request);
    Task<int> GenerateDueScheduledTasksAsync(string gymCode);
    Task DeleteMaintenanceTaskAsync(string gymCode, Guid id);
    Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode);
    Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request);
    Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode);
    Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request);
    Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName);
}
