namespace App.DTO.v1.TreatmentRooms;

public class TreatmentRoomResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public bool IsActiveRoom { get; set; }
}
