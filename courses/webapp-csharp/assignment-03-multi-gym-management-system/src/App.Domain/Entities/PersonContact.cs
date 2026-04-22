using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class PersonContact : BaseEntity
{
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }

    [MaxLength(64)]
    public string? Label { get; set; }

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }
}
