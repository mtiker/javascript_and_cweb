namespace App.DTO.v1.CompanySettings;

public class CompanySettingsResponse
{
    public Guid CompanyId { get; set; }
    public string CountryCode { get; set; } = default!;
    public string CurrencyCode { get; set; } = default!;
    public string Timezone { get; set; } = default!;
    public int DefaultXrayIntervalMonths { get; set; }
}
