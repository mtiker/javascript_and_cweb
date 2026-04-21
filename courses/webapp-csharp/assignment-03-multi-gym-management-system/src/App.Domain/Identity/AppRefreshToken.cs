using App.Domain.Common;

namespace App.Domain.Identity;

public class AppRefreshToken : BaseEntity
{
    public string RefreshToken { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Expiration { get; set; }
    public string? PreviousRefreshToken { get; set; }
    public DateTime? PreviousExpiration { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
}
