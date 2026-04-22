using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;

namespace App.Domain.Entities;

public class OpeningHoursException : TenantBaseEntity
{
    public DateOnly ExceptionDate { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr? Reason { get; set; }
}
