using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.MembershipPackages;

public class MembershipPackageUpsertRequest
{
    public string? Name { get; set; }
    public MembershipPackageType PackageType { get; set; }
    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public decimal BasePrice { get; set; }
    public string? CurrencyCode { get; set; }
    public int? TrainingDiscountPercent { get; set; }
    public bool IsTrainingFree { get; set; }
    public string? Description { get; set; }
}
