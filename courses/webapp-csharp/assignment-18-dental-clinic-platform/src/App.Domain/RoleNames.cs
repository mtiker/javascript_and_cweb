namespace App.Domain;

public static class RoleNames
{
    public const string SystemAdmin = "SystemAdmin";
    public const string SystemSupport = "SystemSupport";
    public const string SystemBilling = "SystemBilling";

    public const string CompanyOwner = "CompanyOwner";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string CompanyManager = "CompanyManager";
    public const string CompanyEmployee = "CompanyEmployee";

    public static readonly string[] All =
    [
        SystemAdmin,
        SystemSupport,
        SystemBilling,
        CompanyOwner,
        CompanyAdmin,
        CompanyManager,
        CompanyEmployee
    ];
}
