using App.Domain.Enums;

namespace App.DTO.v1.OpeningHours;

public class OpeningHoursResponse
{
    public Guid Id { get; set; }
    public int Weekday { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
}
