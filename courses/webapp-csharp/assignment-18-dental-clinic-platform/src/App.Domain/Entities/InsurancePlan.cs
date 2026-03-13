using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class InsurancePlan : TenantBaseEntity
{
    public string Name { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public CoverageType CoverageType { get; set; }
    public bool IsActivePlan { get; set; } = true;
    public string? ClaimSubmissionEndpoint { get; set; }
}
