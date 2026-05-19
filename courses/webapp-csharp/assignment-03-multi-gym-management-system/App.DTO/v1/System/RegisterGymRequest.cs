using App.Domain.Enums;

namespace App.DTO.v1.System;

public class RegisterGymRequest
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? RegistrationCode { get; set; }
    public string AddressLine { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public string Country { get; set; } = "Estonia";
    public string OwnerEmail { get; set; } = default!;
    public string OwnerPassword { get; set; } = default!;
    public string OwnerFirstName { get; set; } = default!;
    public string OwnerLastName { get; set; } = default!;
}
