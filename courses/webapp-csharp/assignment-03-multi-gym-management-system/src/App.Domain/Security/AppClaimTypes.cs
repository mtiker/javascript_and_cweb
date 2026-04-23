namespace App.Domain.Security;

public static class AppClaimTypes
{
    public const string GymId = "active_gym_id";
    public const string GymCode = "active_gym_code";
    public const string ActiveRole = "active_role";
    public const string PersonId = "person_id";
    public const string ImpersonatedUserId = "impersonated_user_id";
    public const string ImpersonatedByUserId = "impersonated_by_user_id";
    public const string ImpersonationReason = "impersonation_reason";
    public const string IsImpersonated = "is_impersonated";
}
