using App.BLL.Contracts.Appointments;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class AppointmentService(AppDbContext dbContext, ITenantAccessService tenantAccessService) : IAppointmentService
{
    public async Task<IReadOnlyCollection<AppointmentResult>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        return await dbContext.Appointments
            .AsNoTracking()
            .OrderBy(entity => entity.StartAtUtc)
            .Select(entity => ToResult(entity))
            .ToListAsync(cancellationToken);
    }

    public async Task<AppointmentResult> CreateAsync(Guid userId, CreateAppointmentCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);
        Validate(command);

        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == command.PatientId, cancellationToken);
        if (!patientExists)
        {
            throw new ValidationAppException("Patient does not exist in current company.");
        }

        var dentistExists = await dbContext.Dentists
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == command.DentistId, cancellationToken);
        if (!dentistExists)
        {
            throw new ValidationAppException("Dentist does not exist in current company.");
        }

        var roomExists = await dbContext.TreatmentRooms
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == command.TreatmentRoomId, cancellationToken);
        if (!roomExists)
        {
            throw new ValidationAppException("Treatment room does not exist in current company.");
        }

        var dentistOverlap = await dbContext.Appointments
            .AsNoTracking()
            .AnyAsync(entity =>
                    entity.DentistId == command.DentistId &&
                    entity.Status != AppointmentStatus.Cancelled &&
                    command.StartAtUtc < entity.EndAtUtc &&
                    command.EndAtUtc > entity.StartAtUtc,
                cancellationToken);
        if (dentistOverlap)
        {
            throw new ValidationAppException("Dentist already has appointment in the selected time range.");
        }

        var roomOverlap = await dbContext.Appointments
            .AsNoTracking()
            .AnyAsync(entity =>
                    entity.TreatmentRoomId == command.TreatmentRoomId &&
                    entity.Status != AppointmentStatus.Cancelled &&
                    command.StartAtUtc < entity.EndAtUtc &&
                    command.EndAtUtc > entity.StartAtUtc,
                cancellationToken);
        if (roomOverlap)
        {
            throw new ValidationAppException("Treatment room already has appointment in the selected time range.");
        }

        var appointment = new Appointment
        {
            PatientId = command.PatientId,
            DentistId = command.DentistId,
            TreatmentRoomId = command.TreatmentRoomId,
            StartAtUtc = command.StartAtUtc,
            EndAtUtc = command.EndAtUtc,
            Status = AppointmentStatus.Scheduled,
            Notes = command.Notes?.Trim()
        };

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResult(appointment);
    }

    private Task EnsureAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        return tenantAccessService.EnsureCompanyRoleAsync(
            userId,
            cancellationToken,
            RoleNames.CompanyOwner,
            RoleNames.CompanyAdmin,
            RoleNames.CompanyManager,
            RoleNames.CompanyEmployee);
    }

    private static void Validate(CreateAppointmentCommand command)
    {
        if (command.StartAtUtc >= command.EndAtUtc)
        {
            throw new ValidationAppException("Appointment start time must be before end time.");
        }

        if (command.StartAtUtc < DateTime.UtcNow.AddYears(-1))
        {
            throw new ValidationAppException("Appointment start time is too far in the past.");
        }
    }

    private static AppointmentResult ToResult(Appointment entity)
    {
        return new AppointmentResult(
            entity.Id,
            entity.PatientId,
            entity.DentistId,
            entity.TreatmentRoomId,
            entity.StartAtUtc,
            entity.EndAtUtc,
            entity.Status.ToString(),
            entity.Notes);
    }
}
