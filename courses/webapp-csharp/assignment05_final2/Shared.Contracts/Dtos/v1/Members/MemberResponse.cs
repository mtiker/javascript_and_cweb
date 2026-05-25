using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Members;

public class MemberResponse
{
    public Guid Id { get; set; }
    public string MemberCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public MemberStatus Status { get; set; }
}
