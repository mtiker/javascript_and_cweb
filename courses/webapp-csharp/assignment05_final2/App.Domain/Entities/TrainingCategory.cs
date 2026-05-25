using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Common;
using Base.Domain;
using Shared.Contracts.Enums;

namespace App.Domain.Entities;

public class TrainingCategory : TenantBaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new("Training", "en");

    [Column(TypeName = "jsonb")]
    public LangStr? Description { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public ICollection<TrainingSession> Sessions { get; set; } = new List<TrainingSession>();
}
