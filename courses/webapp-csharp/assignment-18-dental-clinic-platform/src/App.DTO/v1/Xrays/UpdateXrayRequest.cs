using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Xrays;

public class UpdateXrayRequest
{
    [Required]
    public DateTime TakenAtUtc { get; set; }

    public DateTime? NextDueAtUtc { get; set; }

    [Required]
    [MaxLength(512)]
    public string StoragePath { get; set; } = default!;

    [MaxLength(512)]
    public string? Notes { get; set; }
}
