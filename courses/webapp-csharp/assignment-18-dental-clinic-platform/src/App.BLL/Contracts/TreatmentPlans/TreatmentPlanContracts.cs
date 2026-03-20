namespace App.BLL.Contracts.TreatmentPlans;

public sealed record TreatmentPlanItemCommand(
    Guid TreatmentTypeId,
    int Sequence,
    string Urgency,
    decimal EstimatedPrice);

public sealed record CreateTreatmentPlanCommand(
    Guid PatientId,
    Guid? DentistId,
    IReadOnlyCollection<TreatmentPlanItemCommand> Items);

public sealed record UpdateTreatmentPlanCommand(
    Guid PlanId,
    Guid? PatientId,
    Guid? DentistId,
    IReadOnlyCollection<TreatmentPlanItemCommand>? Items);

public sealed record TreatmentPlanItemResult(
    Guid Id,
    Guid TreatmentTypeId,
    string TreatmentTypeName,
    int Sequence,
    string Urgency,
    decimal EstimatedPrice,
    string Decision,
    DateTime? DecisionAtUtc,
    string? DecisionNotes);

public sealed record TreatmentPlanResult(
    Guid Id,
    Guid PatientId,
    Guid? DentistId,
    string Status,
    DateTime? SubmittedAtUtc,
    DateTime? ApprovedAtUtc,
    bool IsLocked,
    IReadOnlyCollection<TreatmentPlanItemResult> Items);

public sealed record OpenPlanItemResult(
    Guid PlanId,
    Guid PlanItemId,
    Guid PatientId,
    string PatientName,
    string TreatmentTypeName,
    int Sequence,
    string Urgency,
    decimal EstimatedPrice);
