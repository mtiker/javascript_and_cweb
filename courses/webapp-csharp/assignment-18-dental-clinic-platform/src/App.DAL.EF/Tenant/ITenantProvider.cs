namespace App.DAL.EF.Tenant;

public interface ITenantProvider
{
    Guid? CompanyId { get; }
    string? CompanySlug { get; }
    bool IgnoreTenantFilter { get; }

    void SetTenant(Guid companyId, string companySlug);
    void SetIgnoreTenantFilter(bool ignore);
    void ClearTenant();
}
