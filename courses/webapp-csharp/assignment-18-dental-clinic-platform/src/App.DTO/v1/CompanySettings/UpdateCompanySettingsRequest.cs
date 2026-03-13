using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.CompanySettings;

public class UpdateCompanySettingsRequest
{
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = "EE";

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; set; } = "EUR";

    [Required]
    [MaxLength(128)]
    public string Timezone { get; set; } = "Europe/Tallinn";

    [Range(3, 60)]
    public int DefaultXrayIntervalMonths { get; set; } = 12;
}
