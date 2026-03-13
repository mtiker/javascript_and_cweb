namespace App.BLL.Contracts;

public sealed record PlanDecisionResult(
    Guid PlanId,
    Guid PlanItemId,
    string PlanStatus,
    string ItemDecision);
