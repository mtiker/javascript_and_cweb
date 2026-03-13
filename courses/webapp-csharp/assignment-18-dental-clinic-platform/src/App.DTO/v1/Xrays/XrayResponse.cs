namespace App.DTO.v1.Xrays;

public class XrayResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateTime TakenAtUtc { get; set; }
    public DateTime? NextDueAtUtc { get; set; }
    public string StoragePath { get; set; } = default!;
    public string? Notes { get; set; }
}
