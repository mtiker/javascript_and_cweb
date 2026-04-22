using App.Domain.Enums;

namespace App.DTO.v1.Vacations;

public class VacationResponse
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public VacationType? VacationType { get; set; }
    public VacationStatus Status { get; set; }
    public string? Comment { get; set; }
}
