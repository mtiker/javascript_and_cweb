using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Members;

public class MemberUpsertRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? PersonalCode { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string MemberCode { get; set; } = default!;
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    /// <summary>
    /// Optional login email. When supplied together with <see cref="Password"/>,
    /// a login account is provisioned (or reused) for the member.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Optional initial password for the provisioned login. Ignored when the
    /// email already belongs to an existing account.
    /// </summary>
    public string? Password { get; set; }
}
