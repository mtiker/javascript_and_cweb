namespace App.Domain;

public static class RoleNames
{
    public const string SystemAdmin = nameof(SystemAdmin);
    public const string SystemSupport = nameof(SystemSupport);
    public const string SystemBilling = nameof(SystemBilling);

    public const string GymOwner = nameof(GymOwner);
    public const string GymAdmin = nameof(GymAdmin);
    public const string Member = nameof(Member);
    public const string Trainer = nameof(Trainer);
    public const string Caretaker = nameof(Caretaker);

    public static readonly string[] SystemRoles =
    [
        SystemAdmin,
        SystemSupport,
        SystemBilling
    ];

    public static readonly string[] TenantRoles =
    [
        GymOwner,
        GymAdmin,
        Member,
        Trainer,
        Caretaker
    ];
}
