using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class EmploymentContract : TenantBaseEntity
{
    public Guid StaffId { get; set; }
    public Staff? Staff { get; set; }

    public Guid PrimaryJobRoleId { get; set; }
    public JobRole? PrimaryJobRole { get; set; }

    public decimal WorkloadPercent { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr? JobDescription { get; set; }

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EndDate { get; set; }
    public ContractStatus ContractStatus { get; set; } = ContractStatus.Active;
    public EmployerType EmployerType { get; set; } = EmployerType.Internal;

    [MaxLength(128)]
    public string? EmployerName { get; set; }

    public ICollection<Vacation> Vacations { get; set; } = new List<Vacation>();
    public ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
}
