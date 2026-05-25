using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Common;
using Base.Domain;
using Shared.Contracts.Enums;
using App.Domain.Identity;

namespace App.Domain.Entities;

public class GymSettings : BaseEntity, ITenantEntity
{
    public Guid GymId { get; set; }
    public Gym? Gym { get; set; }

    [MaxLength(64)]
    public string CurrencyCode { get; set; } = "EUR";

    [MaxLength(64)]
    public string TimeZone { get; set; } = "Europe/Tallinn";

    public bool AllowNonMemberBookings { get; set; } = true;
    public int BookingCancellationHours { get; set; } = 6;

    [Column(TypeName = "jsonb")]
    public LangStr PublicDescription { get; set; } = new("Gym operations workspace", "en");
}
