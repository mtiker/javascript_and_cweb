using App.BLL.Contracts.Patients;

namespace App.BLL.Services;

public interface IPatientService
{
    Task<IReadOnlyCollection<PatientResult>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<PatientResult> GetAsync(Guid userId, Guid patientId, CancellationToken cancellationToken);
    Task<PatientResult> CreateAsync(Guid userId, CreatePatientCommand command, CancellationToken cancellationToken);
    Task<PatientResult> UpdateAsync(Guid userId, UpdatePatientCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid patientId, CancellationToken cancellationToken);
}
