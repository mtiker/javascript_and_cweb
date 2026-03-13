using App.DAL.EF.Tenant;

namespace WebApp.Helpers;

public class RequestTenantProvider : ITenantProvider
{
    public Guid? CompanyId { get; private set; }
    public string? CompanySlug { get; private set; }
    public bool IgnoreTenantFilter { get; private set; } = true;

    public void SetTenant(Guid companyId, string companySlug)
    {
        CompanyId = companyId;
        CompanySlug = companySlug;
    }

    public void SetIgnoreTenantFilter(bool ignore)
    {
        IgnoreTenantFilter = ignore;
    }

    public void ClearTenant()
    {
        CompanyId = null;
        CompanySlug = null;
        IgnoreTenantFilter = true;
    }
}
