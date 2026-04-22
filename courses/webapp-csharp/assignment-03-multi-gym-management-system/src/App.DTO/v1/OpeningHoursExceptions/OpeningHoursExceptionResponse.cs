using App.Domain.Enums;

namespace App.DTO.v1.OpeningHoursExceptions;

public class OpeningHoursExceptionResponse
{
    public Guid Id { get; set; }
    public DateOnly ExceptionDate { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }
    public string? Reason { get; set; }
}
