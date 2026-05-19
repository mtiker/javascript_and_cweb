namespace App.DTO.v1.TrainingSessions;

public class TrainingSessionRescheduleRequest
{
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
}
