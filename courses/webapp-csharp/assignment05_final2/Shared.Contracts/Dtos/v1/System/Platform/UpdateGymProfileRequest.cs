namespace Shared.Contracts.Dtos.v1.System.Platform;

public class UpdateGymProfileRequest
{
    public string Name { get; set; } = default!;
    public string? RegistrationCode { get; set; }
    public string AddressLine { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public string Country { get; set; } = "Estonia";
    public bool IsActive { get; set; }
}
