namespace App.BLL.Contracts;

public sealed record RegisterCompanyResult(
    Guid CompanyId,
    Guid OwnerUserId,
    string CompanySlug,
    string SubscriptionTier);
