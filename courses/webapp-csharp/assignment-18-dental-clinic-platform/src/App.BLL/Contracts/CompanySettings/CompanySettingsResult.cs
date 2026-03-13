namespace App.BLL.Contracts.CompanySettings;

public sealed record CompanySettingsResult(
    Guid CompanyId,
    string CountryCode,
    string CurrencyCode,
    string Timezone,
    int DefaultXrayIntervalMonths);
