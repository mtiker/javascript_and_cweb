namespace App.DTO.v1.System;

public class RegisterCompanyResponse
{
    public Guid CompanyId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string SubscriptionTier { get; set; } = default!;
}
