using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class Contact : BaseEntity
{
    public ContactType Type { get; set; }

    [MaxLength(128)]
    public string Value { get; set; } = default!;

    public ICollection<PersonContact> PersonLinks { get; set; } = new List<PersonContact>();
    public ICollection<GymContact> GymLinks { get; set; } = new List<GymContact>();
}
