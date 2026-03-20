using App.BLL.Contracts.Finance;
using App.BLL.Contracts.TreatmentPlans;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class FinanceWorkspaceService(AppDbContext dbContext, ITenantAccessService tenantAccessService) : IFinanceWorkspaceService
{
    public async Task<FinanceWorkspaceResult> GetWorkspaceAsync(Guid userId, Guid patientId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);

        var patient = await dbContext.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == patientId, cancellationToken);
        if (patient == null)
        {
            throw new NotFoundException("Patient was not found.");
        }

        var insurancePlans = await dbContext.InsurancePlans
            .AsNoTracking()
            .OrderBy(entity => entity.Name)
            .ToListAsync(cancellationToken);

        var plans = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Where(entity => entity.PatientId == patientId)
            .Include(entity => entity.Items)
            .ThenInclude(entity => entity.TreatmentType)
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var policies = await dbContext.PatientInsurancePolicies
            .AsNoTracking()
            .Where(entity => entity.PatientId == patientId)
            .Include(entity => entity.InsurancePlan)
            .OrderByDescending(entity => entity.CoverageStart)
            .ToListAsync(cancellationToken);

        var estimates = await dbContext.CostEstimates
            .AsNoTracking()
            .Where(entity => entity.PatientId == patientId)
            .OrderByDescending(entity => entity.GeneratedAtUtc)
            .ToListAsync(cancellationToken);

        var procedures = await dbContext.Treatments
            .AsNoTracking()
            .Where(entity => entity.PatientId == patientId)
            .Include(entity => entity.TreatmentType)
            .OrderByDescending(entity => entity.PerformedAtUtc)
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Where(entity => entity.PatientId == patientId)
            .Include(entity => entity.Lines)
            .Include(entity => entity.Payments)
            .OrderByDescending(entity => entity.DueDateUtc)
            .ToListAsync(cancellationToken);

        return new FinanceWorkspaceResult(
            new FinancePatientResult(
                patient.Id,
                patient.FirstName,
                patient.LastName,
                patient.DateOfBirth,
                patient.PersonalCode,
                patient.Email,
                patient.Phone),
            insurancePlans.Select(entity => new InsurancePlanResult(
                entity.Id,
                entity.Name,
                entity.CountryCode,
                entity.CoverageType.ToString(),
                entity.IsActivePlan,
                entity.ClaimSubmissionEndpoint)).ToArray(),
            plans.Select(ToTreatmentPlanResult).ToArray(),
            policies.Select(entity => new PatientInsurancePolicyResult(
                entity.Id,
                entity.PatientId,
                entity.InsurancePlanId,
                entity.InsurancePlan?.Name ?? "-",
                entity.PolicyNumber,
                entity.MemberNumber,
                entity.GroupNumber,
                entity.CoverageStart,
                entity.CoverageEnd,
                entity.AnnualMaximum,
                entity.Deductible,
                entity.CoveragePercent,
                entity.Status.ToString())).ToArray(),
            estimates.Select(CostEstimateService.ToResult).ToArray(),
            procedures.Select(entity => new PerformedProcedureResult(
                entity.Id,
                entity.PatientId,
                entity.TreatmentTypeId,
                entity.PlanItemId,
                entity.AppointmentId,
                entity.ToothNumber,
                entity.PerformedAtUtc,
                entity.Price,
                entity.TreatmentType?.Name ?? "Procedure",
                entity.Notes)).ToArray(),
            invoices.Select(InvoiceService.ToSummaryResult).ToArray());
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

    private static TreatmentPlanResult ToTreatmentPlanResult(TreatmentPlan entity)
    {
        return new TreatmentPlanResult(
            entity.Id,
            entity.PatientId,
            entity.DentistId,
            entity.Status.ToString(),
            entity.SubmittedAtUtc,
            entity.ApprovedAtUtc,
            TreatmentPlanWorkflow.IsLockedForItemReplacement(entity),
            entity.Items
                .OrderBy(item => item.Sequence)
                .Select(item => new TreatmentPlanItemResult(
                    item.Id,
                    item.TreatmentTypeId,
                    item.TreatmentType?.Name ?? "-",
                    item.Sequence,
                    item.Urgency.ToString(),
                    item.EstimatedPrice,
                    item.Decision.ToString(),
                    item.DecisionAtUtc,
                    item.DecisionNotes))
                .ToArray());
    }
}
