using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.System.Platform;

public class UpdateFeatureFlagRequest
{
    [Required]
    [MaxLength(80)]
    public string Key { get; set; } = default!;

    public bool Enabled { get; set; }
}
