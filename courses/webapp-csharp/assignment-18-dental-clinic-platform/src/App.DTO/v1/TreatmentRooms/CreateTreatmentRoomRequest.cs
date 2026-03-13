using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.TreatmentRooms;

public class CreateTreatmentRoomRequest
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = default!;

    [Required]
    [MaxLength(32)]
    public string Code { get; set; } = default!;

    public bool IsActiveRoom { get; set; } = true;
}
