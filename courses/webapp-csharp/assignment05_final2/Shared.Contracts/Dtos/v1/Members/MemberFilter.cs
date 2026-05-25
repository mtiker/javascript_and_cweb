using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos.v1.Members;

public class MemberFilter
{
    public string? Search { get; set; }
    public MemberStatus? Status { get; set; }
}
