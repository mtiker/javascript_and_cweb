namespace App.DTO.v1.InsurancePlans;

public class InsurancePlanResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public string CoverageType { get; set; } = default!;
    public bool IsActivePlan { get; set; }
    public string? ClaimSubmissionEndpoint { get; set; }
}
