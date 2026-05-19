using App.Domain.Enums;

namespace App.DTO.v1.Members;

public class MemberStatusUpdateRequest
{
    public MemberStatus Status { get; set; }
}
