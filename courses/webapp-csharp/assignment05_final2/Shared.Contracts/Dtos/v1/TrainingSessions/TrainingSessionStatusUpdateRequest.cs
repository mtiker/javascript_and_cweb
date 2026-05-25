using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.TrainingSessions;

public class TrainingSessionStatusUpdateRequest
{
    public TrainingSessionStatus Status { get; set; }
}
