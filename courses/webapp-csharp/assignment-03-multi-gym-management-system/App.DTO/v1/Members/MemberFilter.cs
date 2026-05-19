using App.Domain.Enums;

namespace App.DTO.v1.Members;

public class MemberFilter
{
    public string? Search { get; set; }
    public MemberStatus? Status { get; set; }
}
