namespace App.DTO.v1.CostEstimates;

public class LegalEstimateResponse
{
    public Guid CostEstimateId { get; set; }
    public string CountryCode { get; set; } = default!;
    public string DocumentType { get; set; } = default!;
    public string GeneratedText { get; set; } = default!;
    public DateTime GeneratedAtUtc { get; set; }
}
