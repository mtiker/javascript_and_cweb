using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class JobRole : TenantBaseEntity
{
    [MaxLength(32)]
    public string Code { get; set; } = default!;

    [Column(TypeName = "jsonb")]
    public LangStr Title { get; set; } = new("Role", "en");

    [Column(TypeName = "jsonb")]
    public LangStr? Description { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<EmploymentContract> Contracts { get; set; } = new List<EmploymentContract>();
}
