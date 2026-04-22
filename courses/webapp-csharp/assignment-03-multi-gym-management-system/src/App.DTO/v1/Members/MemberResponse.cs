using App.Domain.Enums;

namespace App.DTO.v1.Members;

public class MemberResponse
{
    public Guid Id { get; set; }
    public string MemberCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public MemberStatus Status { get; set; }
}
