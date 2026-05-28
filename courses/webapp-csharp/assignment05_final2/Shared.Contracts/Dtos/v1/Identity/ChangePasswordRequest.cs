namespace Shared.Contracts.Dtos.v1.Identity;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
