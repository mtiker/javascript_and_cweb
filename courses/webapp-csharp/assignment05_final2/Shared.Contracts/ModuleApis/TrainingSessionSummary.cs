namespace Shared.Contracts.ModuleApis;

/// <summary>
/// Public projection of a training session owned by the Training module.
/// Cross-module consumers should depend on this shape instead of EF entities.
/// </summary>
public sealed record TrainingSessionSummary(
    Guid Id,
    Guid GymId,
    Guid CategoryId,
    Guid? TrainerStaffId,
    string Name,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    int Capacity,
    string Status);
