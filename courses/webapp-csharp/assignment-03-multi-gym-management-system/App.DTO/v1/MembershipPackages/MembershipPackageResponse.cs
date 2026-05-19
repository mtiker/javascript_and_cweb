using App.Domain.Enums;

namespace App.DTO.v1.MembershipPackages;

public class MembershipPackageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public MembershipPackageType PackageType { get; set; }
    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public int? TrainingDiscountPercent { get; set; }
    public bool IsTrainingFree { get; set; }
    public string? Description { get; set; }
}
