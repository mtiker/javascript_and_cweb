using App.Domain.Enums;

namespace App.DTO.v1.MembershipPackages;

public class MembershipPackageUpsertRequest
{
    public string Name { get; set; } = default!;
    public MembershipPackageType PackageType { get; set; }
    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public int? TrainingDiscountPercent { get; set; }
    public bool IsTrainingFree { get; set; }
    public string? Description { get; set; }
}
