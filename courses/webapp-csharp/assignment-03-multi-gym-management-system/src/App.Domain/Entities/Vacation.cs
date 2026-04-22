using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class Vacation : TenantBaseEntity
{
    public Guid ContractId { get; set; }
    public EmploymentContract? Contract { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public VacationType? VacationType { get; set; }
    public VacationStatus Status { get; set; } = VacationStatus.Planned;

    [MaxLength(512)]
    public string? Comment { get; set; }
}
