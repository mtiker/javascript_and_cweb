using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class MembershipPackage : TenantBaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new("Package", "en");

    public MembershipPackageType PackageType { get; set; }
    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public decimal BasePrice { get; set; }

    [MaxLength(8)]
    public string CurrencyCode { get; set; } = "EUR";

    public int? TrainingDiscountPercent { get; set; }
    public bool IsTrainingFree { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr? Description { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
