namespace App.BLL.Contracts;

public sealed record RegisterCompanyCommand(
    string CompanyName,
    string CompanySlug,
    string OwnerEmail,
    string OwnerPassword,
    string CountryCode);
