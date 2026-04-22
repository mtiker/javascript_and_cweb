using App.Domain.Enums;

namespace App.DTO.v1.TrainingSessions;

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
