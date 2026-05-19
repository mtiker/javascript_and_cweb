using App.Domain.Enums;

namespace App.DTO.v1.TrainingSessions;

public class TrainingSessionStatusUpdateRequest
{
    public TrainingSessionStatus Status { get; set; }
}
