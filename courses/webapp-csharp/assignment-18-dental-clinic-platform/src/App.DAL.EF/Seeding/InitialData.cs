using App.Domain;

namespace App.DAL.EF.Seeding;

public static class InitialData
{
    public static readonly string[] Roles = RoleNames.All;

    public static readonly (string email, string password, string[] roles)[] Users =
    [
        ("sysadmin@dental-saas.local", "Dental.Saas.101", [RoleNames.SystemAdmin]),
        ("support@dental-saas.local", "Dental.Saas.101", [RoleNames.SystemSupport]),
        ("billing@dental-saas.local", "Dental.Saas.101", [RoleNames.SystemBilling])
    ];
}
