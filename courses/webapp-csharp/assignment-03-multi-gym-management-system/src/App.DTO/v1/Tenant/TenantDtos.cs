using App.Domain.Enums;

namespace App.DTO.v1.Tenant;

public class MemberUpsertRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? PersonalCode { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string MemberCode { get; set; } = default!;
    public MemberStatus Status { get; set; } = MemberStatus.Active;
}

public class MemberResponse
{
    public Guid Id { get; set; }
    public string MemberCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public MemberStatus Status { get; set; }
}

public class MemberDetailResponse
{
    public Guid Id { get; set; }
    public string MemberCode { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? PersonalCode { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public MemberStatus Status { get; set; }
}

public class StaffUpsertRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string StaffCode { get; set; } = default!;
    public StaffStatus Status { get; set; } = StaffStatus.Active;
}

public class StaffResponse
{
    public Guid Id { get; set; }
    public string StaffCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public StaffStatus Status { get; set; }
}

public class JobRoleUpsertRequest
{
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
}

public class JobRoleResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
}

public class ContractUpsertRequest
{
    public Guid StaffId { get; set; }
    public Guid PrimaryJobRoleId { get; set; }
    public decimal WorkloadPercent { get; set; }
    public string? JobDescription { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ContractStatus ContractStatus { get; set; } = ContractStatus.Active;
    public EmployerType EmployerType { get; set; } = EmployerType.Internal;
    public string? EmployerName { get; set; }
}

public class ContractResponse
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public Guid PrimaryJobRoleId { get; set; }
    public decimal WorkloadPercent { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ContractStatus ContractStatus { get; set; }
    public EmployerType EmployerType { get; set; }
}

public class VacationUpsertRequest
{
    public Guid ContractId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public VacationType? VacationType { get; set; }
    public VacationStatus Status { get; set; } = VacationStatus.Planned;
    public string? Comment { get; set; }
}

public class VacationResponse
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public VacationType? VacationType { get; set; }
    public VacationStatus Status { get; set; }
    public string? Comment { get; set; }
}

public class TrainingCategoryUpsertRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class TrainingCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class TrainingSessionUpsertRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public TrainingSessionStatus Status { get; set; } = TrainingSessionStatus.Draft;
    public List<Guid> TrainerContractIds { get; set; } = [];
}

public class TrainingSessionResponse
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public TrainingSessionStatus Status { get; set; }
    public List<Guid> TrainerContractIds { get; set; } = [];
}

public class WorkShiftUpsertRequest
{
    public Guid ContractId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public ShiftType ShiftType { get; set; }
    public Guid? TrainingSessionId { get; set; }
    public string? Comment { get; set; }
}

public class WorkShiftResponse
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public ShiftType ShiftType { get; set; }
    public Guid? TrainingSessionId { get; set; }
    public string? Comment { get; set; }
}

public class BookingCreateRequest
{
    public Guid TrainingSessionId { get; set; }
    public Guid MemberId { get; set; }
    public string? PaymentReference { get; set; }
}

public class AttendanceUpdateRequest
{
    public BookingStatus Status { get; set; }
}

public class BookingResponse
{
    public Guid Id { get; set; }
    public Guid TrainingSessionId { get; set; }
    public Guid MemberId { get; set; }
    public BookingStatus Status { get; set; }
    public decimal ChargedPrice { get; set; }
    public bool PaymentRequired { get; set; }
}

public class MembershipPackageUpsertRequest
{
    public string Name { get; set; } = default!;
    public MembershipPackageType PackageType { get; set; }
    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public int? TrainingDiscountPercent { get; set; }
    public bool IsTrainingFree { get; set; }
    public string? Description { get; set; }
}

public class MembershipPackageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public MembershipPackageType PackageType { get; set; }
    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public int? TrainingDiscountPercent { get; set; }
    public bool IsTrainingFree { get; set; }
    public string? Description { get; set; }
}

public class SellMembershipRequest
{
    public Guid MemberId { get; set; }
    public Guid MembershipPackageId { get; set; }
    public DateOnly? RequestedStartDate { get; set; }
    public string? PaymentReference { get; set; }
}

public class MembershipSaleResponse
{
    public Guid MembershipId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool OverlapDetected { get; set; }
    public DateOnly? SuggestedStartDate { get; set; }
}

public class MembershipResponse
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid MembershipPackageId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal PriceAtPurchase { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public MembershipStatus Status { get; set; }
}

public class PaymentCreateRequest
{
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string? Reference { get; set; }
    public Guid? MembershipId { get; set; }
    public Guid? BookingId { get; set; }
}

public class PaymentResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public DateTime PaidAtUtc { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Reference { get; set; }
    public Guid? MembershipId { get; set; }
    public Guid? BookingId { get; set; }
}

public class OpeningHoursUpsertRequest
{
    public int Weekday { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
}

public class OpeningHoursResponse
{
    public Guid Id { get; set; }
    public int Weekday { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
}

public class OpeningHoursExceptionUpsertRequest
{
    public DateOnly ExceptionDate { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }
    public string? Reason { get; set; }
}

public class OpeningHoursExceptionResponse
{
    public Guid Id { get; set; }
    public DateOnly ExceptionDate { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }
    public string? Reason { get; set; }
}

public class EquipmentModelUpsertRequest
{
    public string Name { get; set; } = default!;
    public EquipmentType Type { get; set; }
    public string? Manufacturer { get; set; }
    public int MaintenanceIntervalDays { get; set; }
    public string? Description { get; set; }
}

public class EquipmentModelResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public EquipmentType Type { get; set; }
    public string? Manufacturer { get; set; }
    public int MaintenanceIntervalDays { get; set; }
    public string? Description { get; set; }
}

public class EquipmentUpsertRequest
{
    public Guid EquipmentModelId { get; set; }
    public string? AssetTag { get; set; }
    public string? SerialNumber { get; set; }
    public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Active;
    public DateOnly? CommissionedAt { get; set; }
    public DateOnly? DecommissionedAt { get; set; }
    public string? Notes { get; set; }
}

public class EquipmentResponse
{
    public Guid Id { get; set; }
    public Guid EquipmentModelId { get; set; }
    public string? AssetTag { get; set; }
    public string? SerialNumber { get; set; }
    public EquipmentStatus CurrentStatus { get; set; }
    public DateOnly? CommissionedAt { get; set; }
    public DateOnly? DecommissionedAt { get; set; }
    public string? Notes { get; set; }
}

public class MaintenanceTaskUpsertRequest
{
    public Guid EquipmentId { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public Guid? CreatedByStaffId { get; set; }
    public MaintenanceTaskType TaskType { get; set; }
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public MaintenanceTaskStatus Status { get; set; } = MaintenanceTaskStatus.Open;
    public DateTime? DueAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class MaintenanceStatusUpdateRequest
{
    public MaintenanceTaskStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class MaintenanceTaskResponse
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public Guid? CreatedByStaffId { get; set; }
    public MaintenanceTaskType TaskType { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceTaskStatus Status { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class GymSettingsUpdateRequest
{
    public string CurrencyCode { get; set; } = "EUR";
    public string TimeZone { get; set; } = "Europe/Tallinn";
    public bool AllowNonMemberBookings { get; set; }
    public int BookingCancellationHours { get; set; }
    public string? PublicDescription { get; set; }
}

public class GymSettingsResponse
{
    public Guid GymId { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public string TimeZone { get; set; } = default!;
    public bool AllowNonMemberBookings { get; set; }
    public int BookingCancellationHours { get; set; }
    public string? PublicDescription { get; set; }
}

public class GymUserUpsertRequest
{
    public Guid AppUserId { get; set; }
    public string RoleName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class GymUserResponse
{
    public Guid AppUserId { get; set; }
    public string Email { get; set; } = default!;
    public string RoleName { get; set; } = default!;
    public bool IsActive { get; set; }
}
