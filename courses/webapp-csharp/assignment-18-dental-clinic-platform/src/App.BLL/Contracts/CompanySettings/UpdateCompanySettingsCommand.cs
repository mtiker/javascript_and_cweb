namespace App.BLL.Contracts.CompanySettings;

public sealed record UpdateCompanySettingsCommand(
    string CountryCode,
    string CurrencyCode,
    string Timezone,
    int DefaultXrayIntervalMonths);
