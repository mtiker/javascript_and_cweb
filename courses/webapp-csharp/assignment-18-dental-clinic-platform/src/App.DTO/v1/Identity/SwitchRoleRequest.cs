using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Identity;

public class SwitchRoleRequest
{
    [Required]
    [MaxLength(64)]
    public string RoleName { get; set; } = default!;
}
