using App.Domain;

namespace App.DAL.EF.Seeding;

public static class InitialData
{
    public const string DefaultPassword = "Dental.Saas.101";

    public static readonly string[] Roles = RoleNames.All;

    public static readonly (string email, string password, string[] roles)[] Users =
    [
        ("sysadmin@dental-saas.local", DefaultPassword, [RoleNames.SystemAdmin]),
        ("support@dental-saas.local", DefaultPassword, [RoleNames.SystemSupport]),
        ("billing@dental-saas.local", DefaultPassword, [RoleNames.SystemBilling]),
        ("owner.demo@dental-saas.local", DefaultPassword, []),
        ("admin.demo@dental-saas.local", DefaultPassword, []),
        ("manager.demo@dental-saas.local", DefaultPassword, []),
        ("employee.demo@dental-saas.local", DefaultPassword, []),
        ("multitenant.demo@dental-saas.local", DefaultPassword, [])
    ];
}
