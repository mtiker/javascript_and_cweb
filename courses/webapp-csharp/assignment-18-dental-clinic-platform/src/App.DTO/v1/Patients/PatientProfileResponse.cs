namespace App.DTO.v1.Patients;

public class PatientProfileResponse : PatientResponse
{
    public IReadOnlyCollection<PatientToothResponse> Teeth { get; set; } = Array.Empty<PatientToothResponse>();
}
