using App.Domain.Enums;

namespace App.DTO.v1.Members;

public class MemberUpsertRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? PersonalCode { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string MemberCode { get; set; } = default!;
    public MemberStatus Status { get; set; } = MemberStatus.Active;
}
