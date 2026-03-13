using App.Domain.Common;

namespace App.Domain.Entities;

public class CompanySettings : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }
    public string CountryCode { get; set; } = "DE";
    public string CurrencyCode { get; set; } = "EUR";
    public string Timezone { get; set; } = "Europe/Berlin";
    public int DefaultXrayIntervalMonths { get; set; } = 12;

    public Company? Company { get; set; }
}
