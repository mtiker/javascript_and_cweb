namespace App.BLL.Contracts;

public sealed record SubmitTreatmentPlanResult(
    Guid PlanId,
    string Status,
    DateTime? SubmittedAtUtc,
    DateTime? ApprovedAtUtc);
