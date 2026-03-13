namespace App.DTO.v1.TreatmentTypes;

public class TreatmentTypeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public int DefaultDurationMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public string? Description { get; set; }
}
