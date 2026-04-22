using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class Person : BaseEntity
{
    [MaxLength(64)]
    public string FirstName { get; set; } = default!;

    [MaxLength(64)]
    public string LastName { get; set; } = default!;

    [MaxLength(32)]
    public string? PersonalCode { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }

    public AppUser? AppUser { get; set; }
    public ICollection<PersonContact> Contacts { get; set; } = new List<PersonContact>();
    public ICollection<Member> MemberProfiles { get; set; } = new List<Member>();
    public ICollection<Staff> StaffProfiles { get; set; } = new List<Staff>();
}
