namespace App.DTO.v1.Appointments;

public class AppointmentClinicalRecordResponse
{
    public Guid AppointmentId { get; set; }
    public string Status { get; set; } = default!;
    public int RecordedItemCount { get; set; }
}
