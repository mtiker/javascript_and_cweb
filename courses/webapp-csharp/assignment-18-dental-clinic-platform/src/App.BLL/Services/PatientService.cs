using App.BLL.Contracts.Patients;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
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

    public async Task<PatientResult> CreateAsync(Guid userId, CreatePatientCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);
        Validate(command.FirstName, command.LastName);
        await subscriptionPolicyService.EnsureCanCreatePatientAsync(cancellationToken);

        var patient = new Patient
        {
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            DateOfBirth = command.DateOfBirth,
            PersonalCode = command.PersonalCode?.Trim(),
            Email = command.Email?.Trim(),
            Phone = command.Phone?.Trim()
        };

        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResult(patient);
    }

    public async Task<PatientResult> UpdateAsync(Guid userId, UpdatePatientCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);
        Validate(command.FirstName, command.LastName);

        var patient = await dbContext.Patients
            .SingleOrDefaultAsync(entity => entity.Id == command.PatientId, cancellationToken);

        if (patient == null)
        {
            throw new NotFoundException("Patient was not found.");
        }

        patient.FirstName = command.FirstName.Trim();
        patient.LastName = command.LastName.Trim();
        patient.DateOfBirth = command.DateOfBirth;
        patient.PersonalCode = command.PersonalCode?.Trim();
        patient.Email = command.Email?.Trim();
        patient.Phone = command.Phone?.Trim();

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
}
