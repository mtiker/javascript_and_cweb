namespace App.DTO.v1.System.Support;

public class CompanySnapshotResponse
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = default!;
    public string CompanySlug { get; set; } = default!;
    public bool IsActive { get; set; }
    public int ActiveUserCount { get; set; }
    public int PatientCount { get; set; }
    public int AppointmentCount { get; set; }
    public int OpenInvoiceCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
