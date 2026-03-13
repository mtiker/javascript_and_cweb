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

    public async Task<AppointmentClinicalRecordResult> RecordClinicalWorkAsync(Guid userId, RecordAppointmentClinicalCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);
        Validate(command);

        var appointment = await dbContext.Appointments
            .SingleOrDefaultAsync(entity => entity.Id == command.AppointmentId, cancellationToken);

        if (appointment == null)
        {
            throw new NotFoundException("Appointment was not found.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new ValidationAppException("Clinical work cannot be recorded for cancelled appointments.");
        }

        var treatmentTypeIds = command.Items
            .Select(entity => entity.TreatmentTypeId)
            .Distinct()
            .ToArray();

        var treatmentTypes = await dbContext.TreatmentTypes
            .AsNoTracking()
            .Where(entity => treatmentTypeIds.Contains(entity.Id))
            .ToDictionaryAsync(entity => entity.Id, cancellationToken);

        if (treatmentTypes.Count != treatmentTypeIds.Length)
        {
            throw new ValidationAppException("One or more treatment types do not exist in current company.");
        }

        var toothNumbers = command.Items
            .Select(entity => entity.ToothNumber)
            .Distinct()
            .ToArray();

        var toothRecords = await dbContext.ToothRecords
            .Where(entity => entity.PatientId == appointment.PatientId && toothNumbers.Contains(entity.ToothNumber))
            .ToDictionaryAsync(entity => entity.ToothNumber, cancellationToken);

        foreach (var item in command.Items)
        {
            var treatmentType = treatmentTypes[item.TreatmentTypeId];
            var record = GetOrCreateToothRecord(toothRecords, appointment.PatientId, item);

            record.Condition = item.Condition;
            record.Notes = NormalizeOptional(item.Notes) ?? treatmentType.Name;

            dbContext.Treatments.Add(new Treatment
            {
                PatientId = appointment.PatientId,
                TreatmentTypeId = item.TreatmentTypeId,
                AppointmentId = appointment.Id,
                DentistId = appointment.DentistId,
                ToothNumber = item.ToothNumber,
                PerformedAtUtc = command.PerformedAtUtc,
                Price = item.Price ?? treatmentType.BasePrice,
                Notes = NormalizeOptional(item.Notes)
            });
        }

        if (command.MarkAppointmentCompleted)
        {
            appointment.Status = AppointmentStatus.Completed;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AppointmentClinicalRecordResult(
            appointment.Id,
            appointment.Status.ToString(),
            command.Items.Count);
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

    private static void Validate(RecordAppointmentClinicalCommand command)
    {
        if (command.Items.Count == 0)
        {
            throw new ValidationAppException("Record at least one tooth entry for the appointment.");
        }

        if (command.PerformedAtUtc < DateTime.UtcNow.AddYears(-5))
        {
            throw new ValidationAppException("Performed date is too far in the past.");
        }

        foreach (var item in command.Items)
        {
            if (!ToothChart.IsValidPermanentToothNumber(item.ToothNumber))
            {
                throw new ValidationAppException($"Tooth number {item.ToothNumber} is not a valid permanent FDI tooth number.");
            }

            if (item.Price.HasValue && item.Price.Value < 0)
            {
                throw new ValidationAppException("Treatment price cannot be negative.");
            }
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

    private ToothRecord GetOrCreateToothRecord(
        IDictionary<int, ToothRecord> toothRecords,
        Guid patientId,
        RecordAppointmentClinicalItemCommand item)
    {
        if (toothRecords.TryGetValue(item.ToothNumber, out var record))
        {
            return record;
        }

        record = new ToothRecord
        {
            PatientId = patientId,
            ToothNumber = item.ToothNumber,
            Condition = item.Condition
        };

        toothRecords[item.ToothNumber] = record;
        dbContext.ToothRecords.Add(record);

        return record;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
