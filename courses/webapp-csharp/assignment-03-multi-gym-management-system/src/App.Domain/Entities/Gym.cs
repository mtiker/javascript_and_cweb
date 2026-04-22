using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Common;
using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class Gym : BaseEntity
{
    [MaxLength(128)]
    public string Name { get; set; } = default!;

    [MaxLength(64)]
    public string Code { get; set; } = default!;

    [MaxLength(64)]
    public string? RegistrationCode { get; set; }

    [MaxLength(128)]
    public string AddressLine { get; set; } = default!;

    [MaxLength(64)]
    public string City { get; set; } = default!;

    [MaxLength(32)]
    public string PostalCode { get; set; } = default!;

    [MaxLength(64)]
    public string Country { get; set; } = "Estonia";

    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;

    public GymSettings? Settings { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
    public ICollection<GymContact> Contacts { get; set; } = new List<GymContact>();
    public ICollection<AppUserGymRole> UserRoles { get; set; } = new List<AppUserGymRole>();
}
