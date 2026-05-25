using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.System.Platform;

public class GymSummaryResponse
{
    public Guid GymId { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public bool IsActive { get; set; }
    public string City { get; set; } = default!;
    public string Country { get; set; } = default!;
}
