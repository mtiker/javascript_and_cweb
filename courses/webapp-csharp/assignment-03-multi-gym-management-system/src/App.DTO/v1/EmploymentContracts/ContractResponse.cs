using App.Domain.Enums;

namespace App.DTO.v1.EmploymentContracts;

public class ContractResponse
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public Guid PrimaryJobRoleId { get; set; }
    public decimal WorkloadPercent { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ContractStatus ContractStatus { get; set; }
    public EmployerType EmployerType { get; set; }
}
