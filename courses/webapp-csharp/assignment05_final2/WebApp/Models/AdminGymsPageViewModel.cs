using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApp.Models;

public class AdminGymsPageViewModel
{
    public IReadOnlyCollection<AdminGymSummaryViewModel> Gyms { get; set; } = [];
}

public class AdminGymSummaryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string City { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class AdminGymFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(128)]
    [Display(Name = "Name")]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(64)]
    [Display(Name = "Code")]
    public string Code { get; set; } = default!;

    [StringLength(64)]
    [Display(Name = "Registration code")]
    public string? RegistrationCode { get; set; }

    [Required]
    [StringLength(128)]
    [Display(Name = "Address")]
    public string AddressLine { get; set; } = default!;

    [Required]
    [StringLength(64)]
    [Display(Name = "City")]
    public string City { get; set; } = default!;

    [Required]
    [StringLength(32)]
    [Display(Name = "Postal code")]
    public string PostalCode { get; set; } = default!;

    [Required]
    [StringLength(64)]
    [Display(Name = "Country")]
    public string Country { get; set; } = "Estonia";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Owner email")]
    public string? OwnerEmail { get; set; }

    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Owner password")]
    public string? OwnerPassword { get; set; }

    [StringLength(100)]
    [Display(Name = "Owner first name")]
    public string? OwnerFirstName { get; set; }

    [StringLength(100)]
    [Display(Name = "Owner last name")]
    public string? OwnerLastName { get; set; }

    public bool IsEdit => Id.HasValue;
}
