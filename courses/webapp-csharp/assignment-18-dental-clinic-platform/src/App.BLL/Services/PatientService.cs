using App.BLL.Contracts.Patients;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class PatientService(
    AppDbContext dbContext,
    ITenantAccessService tenantAccessService,
    ISubscriptionPolicyService subscriptionPolicyService) : IPatientService
{
    public async Task<IReadOnlyCollection<PatientResult>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        return await dbContext.Patients
            .AsNoTracking()
            .OrderBy(entity => entity.LastName)
            .ThenBy(entity => entity.FirstName)
            .Select(entity => ToResult(entity))
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientResult> GetAsync(Guid userId, Guid patientId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var patient = await dbContext.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == patientId, cancellationToken);

        if (patient == null)
        {
            throw new NotFoundException("Patient was not found.");
        }

        return ToResult(patient);
    }

    public async Task<PatientProfileResult> GetProfileAsync(Guid userId, Guid patientId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var patient = await dbContext.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == patientId, cancellationToken);

        if (patient == null)
        {
            throw new NotFoundException("Patient was not found.");
        }

        await EnsureFullToothChartAsync(patientId, cancellationToken);

        var toothRecords = await dbContext.ToothRecords
            .AsNoTracking()
            .Where(entity => entity.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var treatments = await dbContext.Treatments
            .AsNoTracking()
            .Where(entity => entity.PatientId == patientId && entity.ToothNumber.HasValue)
            .Include(entity => entity.TreatmentType)
            .OrderByDescending(entity => entity.PerformedAtUtc)
            .ToListAsync(cancellationToken);

        var toothRecordByNumber = toothRecords.ToDictionary(entity => entity.ToothNumber);
        var historyByToothNumber = treatments
            .Where(entity => entity.ToothNumber.HasValue)
            .GroupBy(entity => entity.ToothNumber!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<PatientToothHistoryItemResult>)group
                    .Select(entity => new PatientToothHistoryItemResult(
                        entity.Id,
                        entity.AppointmentId,
                        entity.TreatmentType?.Name ?? "Treatment",
                        entity.PerformedAtUtc,
                        entity.Price,
                        entity.Notes))
                    .ToArray());

        var teeth = ToothChart.PermanentToothNumbers
            .Select(toothNumber =>
            {
                toothRecordByNumber.TryGetValue(toothNumber, out var record);
                historyByToothNumber.TryGetValue(toothNumber, out var history);

                var safeRecord = record ?? new ToothRecord
                {
                    PatientId = patientId,
                    ToothNumber = toothNumber,
                    Condition = ToothConditionStatus.Healthy
                };
                var safeHistory = history ?? Array.Empty<PatientToothHistoryItemResult>();
                var latestHistory = safeHistory.FirstOrDefault();

                return new PatientToothResult(
                    safeRecord.Id,
                    toothNumber,
                    safeRecord.Condition.ToString(),
                    safeRecord.Notes,
                    safeRecord.ModifiedAtUtc ?? safeRecord.CreatedAtUtc,
                    latestHistory?.PerformedAtUtc,
                    latestHistory?.TreatmentTypeName,
                    latestHistory?.Notes,
                    safeHistory);
            })
            .ToArray();

        return new PatientProfileResult(
            patient.Id,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.PersonalCode,
            patient.Email,
            patient.Phone,
            teeth);
    }

    public async Task<PatientResult> CreateAsync(Guid userId, CreatePatientCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);
        Validate(command.FirstName, command.LastName);
        var personalCode = NormalizeOptional(command.PersonalCode);
        await EnsurePersonalCodeIsAvailableAsync(personalCode, null, cancellationToken);
        await subscriptionPolicyService.EnsureCanCreatePatientAsync(cancellationToken);

        var patient = new Patient
        {
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            DateOfBirth = command.DateOfBirth,
            PersonalCode = personalCode,
            Email = NormalizeOptional(command.Email),
            Phone = NormalizeOptional(command.Phone)
        };

        dbContext.Patients.Add(patient);
        dbContext.ToothRecords.AddRange(BuildDefaultToothRecords(patient.Id));
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResult(patient);
    }

    public async Task<PatientResult> UpdateAsync(Guid userId, UpdatePatientCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);
        Validate(command.FirstName, command.LastName);
        var personalCode = NormalizeOptional(command.PersonalCode);
        await EnsurePersonalCodeIsAvailableAsync(personalCode, command.PatientId, cancellationToken);

        var patient = await dbContext.Patients
            .SingleOrDefaultAsync(entity => entity.Id == command.PatientId, cancellationToken);

        if (patient == null)
        {
            throw new NotFoundException("Patient was not found.");
        }

        patient.FirstName = command.FirstName.Trim();
        patient.LastName = command.LastName.Trim();
        patient.DateOfBirth = command.DateOfBirth;
        patient.PersonalCode = personalCode;
        patient.Email = NormalizeOptional(command.Email);
        patient.Phone = NormalizeOptional(command.Phone);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResult(patient);
    }

    public async Task DeleteAsync(Guid userId, Guid patientId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var patient = await dbContext.Patients
            .SingleOrDefaultAsync(entity => entity.Id == patientId, cancellationToken);

        if (patient == null)
        {
            throw new NotFoundException("Patient was not found.");
        }

        await SoftDeletePatientRelationsAsync(patientId, cancellationToken);
        dbContext.Patients.Remove(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
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

    private static void Validate(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ValidationAppException("Patient first name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ValidationAppException("Patient last name is required.");
        }
    }

    private async Task EnsurePersonalCodeIsAvailableAsync(
        string? personalCode,
        Guid? currentPatientId,
        CancellationToken cancellationToken)
    {
        if (personalCode == null)
        {
            return;
        }

        var conflictExists = await dbContext.Patients
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(entity =>
                    !entity.IsDeleted &&
                    entity.PersonalCode == personalCode &&
                    (!currentPatientId.HasValue || entity.Id != currentPatientId.Value),
                cancellationToken);

        if (conflictExists)
        {
            throw new ValidationAppException(
                "An active patient with the same personal code already exists. Remove the patient from the previous clinic before adding them to a new clinic.");
        }
    }

    private static PatientResult ToResult(Patient entity)
    {
        return new PatientResult(
            entity.Id,
            entity.FirstName,
            entity.LastName,
            entity.DateOfBirth,
            entity.PersonalCode,
            entity.Email,
            entity.Phone);
    }

    private static IReadOnlyCollection<ToothRecord> BuildDefaultToothRecords(Guid patientId)
    {
        return ToothChart.PermanentToothNumbers
            .Select(toothNumber => new ToothRecord
            {
                PatientId = patientId,
                ToothNumber = toothNumber,
                Condition = ToothConditionStatus.Healthy
            })
            .ToArray();
    }

    private async Task EnsureFullToothChartAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var existingToothNumbers = await dbContext.ToothRecords
            .Where(entity => entity.PatientId == patientId)
            .Select(entity => entity.ToothNumber)
            .ToListAsync(cancellationToken);

        var missingToothNumbers = ToothChart.PermanentToothNumbers
            .Except(existingToothNumbers)
            .ToArray();

        if (missingToothNumbers.Length == 0)
        {
            return;
        }

        dbContext.ToothRecords.AddRange(missingToothNumbers.Select(toothNumber => new ToothRecord
        {
            PatientId = patientId,
            ToothNumber = toothNumber,
            Condition = ToothConditionStatus.Healthy
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SoftDeletePatientRelationsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var toothRecords = await dbContext.ToothRecords
            .Where(entity => entity.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var treatments = await dbContext.Treatments
            .Where(entity => entity.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var appointments = await dbContext.Appointments
            .Where(entity => entity.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var xrays = await dbContext.Xrays
            .Where(entity => entity.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var treatmentPlans = await dbContext.TreatmentPlans
            .Where(entity => entity.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var treatmentPlanIds = treatmentPlans.Select(entity => entity.Id).ToArray();
        var planItems = treatmentPlanIds.Length == 0
            ? new List<PlanItem>()
            : await dbContext.PlanItems
                .Where(entity => treatmentPlanIds.Contains(entity.TreatmentPlanId))
                .ToListAsync(cancellationToken);

        var costEstimates = await dbContext.CostEstimates
            .Where(entity => entity.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.Invoices
            .Where(entity => entity.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(entity => entity.Id).ToArray();
        var invoiceLines = invoiceIds.Length == 0
            ? new List<InvoiceLine>()
            : await dbContext.InvoiceLines
                .Where(entity => invoiceIds.Contains(entity.InvoiceId))
                .ToListAsync(cancellationToken);
        var payments = invoiceIds.Length == 0
            ? new List<Payment>()
            : await dbContext.Payments
                .Where(entity => invoiceIds.Contains(entity.InvoiceId))
                .ToListAsync(cancellationToken);
        var paymentPlans = invoiceIds.Length == 0
            ? new List<PaymentPlan>()
            : await dbContext.PaymentPlans
                .Where(entity => invoiceIds.Contains(entity.InvoiceId))
                .ToListAsync(cancellationToken);
        var paymentPlanIds = paymentPlans.Select(entity => entity.Id).ToArray();
        var paymentPlanInstallments = paymentPlanIds.Length == 0
            ? new List<PaymentPlanInstallment>()
            : await dbContext.PaymentPlanInstallments
                .Where(entity => paymentPlanIds.Contains(entity.PaymentPlanId))
                .ToListAsync(cancellationToken);
        var patientInsurancePolicies = await dbContext.PatientInsurancePolicies
            .Where(entity => entity.PatientId == patientId)
            .ToListAsync(cancellationToken);

        dbContext.ToothRecords.RemoveRange(toothRecords);
        dbContext.InvoiceLines.RemoveRange(invoiceLines);
        dbContext.Payments.RemoveRange(payments);
        dbContext.PaymentPlanInstallments.RemoveRange(paymentPlanInstallments);
        dbContext.Treatments.RemoveRange(treatments);
        dbContext.Appointments.RemoveRange(appointments);
        dbContext.Xrays.RemoveRange(xrays);
        dbContext.PlanItems.RemoveRange(planItems);
        dbContext.TreatmentPlans.RemoveRange(treatmentPlans);
        dbContext.CostEstimates.RemoveRange(costEstimates);
        dbContext.PaymentPlans.RemoveRange(paymentPlans);
        dbContext.PatientInsurancePolicies.RemoveRange(patientInsurancePolicies);
        dbContext.Invoices.RemoveRange(invoices);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
