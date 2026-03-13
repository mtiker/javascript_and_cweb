using App.Domain.Enums;

namespace App.BLL.Contracts;

public sealed record RecordPlanItemDecisionCommand(
    Guid PlanId,
    Guid PlanItemId,
    PlanItemDecision Decision,
    string? Notes);
