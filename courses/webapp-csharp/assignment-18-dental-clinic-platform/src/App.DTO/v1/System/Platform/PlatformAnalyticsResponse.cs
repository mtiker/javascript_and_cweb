namespace App.DTO.v1.System.Platform;

public class PlatformAnalyticsResponse
{
    public int CompanyCount { get; set; }
    public int ActiveCompanyCount { get; set; }
    public int UserCount { get; set; }
    public int PatientCount { get; set; }
    public int AppointmentCount { get; set; }
    public int InvoiceCount { get; set; }
}
