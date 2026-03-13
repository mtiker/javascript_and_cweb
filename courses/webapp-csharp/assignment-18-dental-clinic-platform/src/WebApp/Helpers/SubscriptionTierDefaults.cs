using App.Domain.Enums;

namespace WebApp.Helpers;

public static class SubscriptionTierDefaults
{
    public static bool TryParseTier(string value, out SubscriptionTier tier)
    {
        return Enum.TryParse(value?.Trim(), true, out tier);
    }

    public static (int userLimit, int entityLimit) ResolveLimits(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Free => (5, 100),
            SubscriptionTier.Standard => (25, 5000),
            SubscriptionTier.Premium => (0, 0),
            _ => (5, 100)
        };
    }
}
