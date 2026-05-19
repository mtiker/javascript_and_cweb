using App.Domain.Enums;

namespace App.DTO.v1.TrainingSessions;

public class TrainingSessionFilter
{
    public TrainingSessionStatus? Status { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? TrainerStaffId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
