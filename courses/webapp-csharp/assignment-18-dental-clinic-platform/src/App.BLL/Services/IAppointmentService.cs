using App.BLL.Contracts.Appointments;

namespace App.BLL.Services;

public interface IAppointmentService
{
    Task<IReadOnlyCollection<AppointmentResult>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<AppointmentResult> CreateAsync(Guid userId, CreateAppointmentCommand command, CancellationToken cancellationToken);
    Task<AppointmentClinicalRecordResult> RecordClinicalWorkAsync(Guid userId, RecordAppointmentClinicalCommand command, CancellationToken cancellationToken);
}
