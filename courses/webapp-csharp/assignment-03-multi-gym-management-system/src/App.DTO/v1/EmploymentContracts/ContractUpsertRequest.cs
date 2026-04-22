using App.Domain.Enums;

namespace App.DTO.v1.EmploymentContracts;

public class ContractUpsertRequest
{
    public Guid StaffId { get; set; }
    public Guid PrimaryJobRoleId { get; set; }
    public decimal WorkloadPercent { get; set; }
    public string? JobDescription { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ContractStatus ContractStatus { get; set; } = ContractStatus.Active;
    public EmployerType EmployerType { get; set; } = EmployerType.Internal;
    public string? EmployerName { get; set; }
}
