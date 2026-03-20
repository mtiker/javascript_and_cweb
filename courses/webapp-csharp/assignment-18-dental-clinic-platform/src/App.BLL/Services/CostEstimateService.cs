using App.BLL.Contracts.Finance;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class CostEstimateService(
    AppDbContext dbContext,
    ITenantAccessService tenantAccessService,
    ISubscriptionPolicyService subscriptionPolicyService) : ICostEstimateService
{
    public async Task<IReadOnlyCollection<CostEstimateResult>> ListAsync(Guid userId, Guid? patientId, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);
        await subscriptionPolicyService.EnsureTierAtLeastAsync("CostEstimates", SubscriptionTier.Standard, cancellationToken);

        var query = dbContext.CostEstimates
            .AsNoTracking()
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(entity => entity.PatientId == patientId.Value);
        }

        return await query
            .OrderByDescending(entity => entity.GeneratedAtUtc)
            .Select(entity => ToResult(entity))
            .ToListAsync(cancellationToken);
    }

    public async Task<CostEstimateResult> CreateAsync(Guid userId, CreateCostEstimateCommand command, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);
        await subscriptionPolicyService.EnsureTierAtLeastAsync("CostEstimates", SubscriptionTier.Standard, cancellationToken);

        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == command.PatientId, cancellationToken);
        if (!patientExists)
        {
            throw new ValidationAppException("Patient does not exist in current company.");
        }

        var plan = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(entity => entity.Items)
            .SingleOrDefaultAsync(entity => entity.Id == command.TreatmentPlanId, cancellationToken);
        if (plan == null)
        {
            throw new ValidationAppException("Treatment plan does not exist in current company.");
        }

        if (plan.PatientId != command.PatientId)
        {
            throw new ValidationAppException("Treatment plan does not belong to the selected patient.");
        }

        if (plan.Items.Count == 0)
        {
            throw new ValidationAppException("Treatment plan must contain at least one item before an estimate can be generated.");
        }

        PatientInsurancePolicy? policy = null;
        if (command.PatientInsurancePolicyId.HasValue)
        {
            policy = await dbContext.PatientInsurancePolicies
                .AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == command.PatientInsurancePolicyId.Value, cancellationToken);
            if (policy == null)
            {
                throw new ValidationAppException("Patient insurance policy does not exist in current company.");
            }

            if (policy.PatientId != command.PatientId)
            {
                throw new ValidationAppException("Patient insurance policy does not belong to the selected patient.");
            }

            if (policy.Status != PatientInsurancePolicyStatus.Active)
            {
                throw new ValidationAppException("Only active insurance policies can be used for cost estimates.");
            }
        }

        var estimateNumber = command.EstimateNumber.Trim();
        var duplicateNumber = await dbContext.CostEstimates
            .AsNoTracking()
            .AnyAsync(entity => entity.EstimateNumber == estimateNumber, cancellationToken);
        if (duplicateNumber)
        {
            throw new ValidationAppException("Estimate number already exists.");
        }

        var breakdown = FinanceMath.CalculateEstimate(plan.Items.Sum(entity => entity.EstimatedPrice), policy);

        var estimate = new CostEstimate
        {
            PatientId = command.PatientId,
            TreatmentPlanId = command.TreatmentPlanId,
            InsurancePlanId = policy?.InsurancePlanId,
            PatientInsurancePolicyId = policy?.Id,
            EstimateNumber = estimateNumber,
            FormatCode = command.FormatCode.Trim().ToUpperInvariant(),
            TotalEstimatedAmount = breakdown.TotalAmount,
            CoverageAmount = breakdown.CoverageAmount,
            PatientEstimatedAmount = breakdown.PatientAmount,
            GeneratedAtUtc = DateTime.UtcNow,
            Status = CostEstimateStatus.Prepared
        };

        dbContext.CostEstimates.Add(estimate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResult(estimate);
    }

    public async Task<LegalEstimateResult> GetLegalAsync(
        Guid userId,
        Guid costEstimateId,
        string? countryCode,
        CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(userId, cancellationToken);
        await subscriptionPolicyService.EnsureTierAtLeastAsync("CostEstimatesLegalOutput", SubscriptionTier.Standard, cancellationToken);

        var estimate = await dbContext.CostEstimates
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == costEstimateId, cancellationToken);
        if (estimate == null)
        {
            throw new NotFoundException("Cost estimate was not found.");
        }

        var effectiveCountryCode = await ResolveCountryCodeAsync(countryCode, cancellationToken);
        var documentType = string.Equals(effectiveCountryCode, "DE", StringComparison.OrdinalIgnoreCase)
            ? "Kostenvoranschlag"
            : "Cost Estimate";

        return new LegalEstimateResult(
            estimate.Id,
            effectiveCountryCode,
            documentType,
            BuildLegalText(documentType, effectiveCountryCode, estimate),
            DateTime.UtcNow);
    }

    internal static CostEstimateResult ToResult(CostEstimate estimate)
    {
        return new CostEstimateResult(
            estimate.Id,
            estimate.PatientId,
            estimate.TreatmentPlanId,
            estimate.PatientInsurancePolicyId,
            estimate.InsurancePlanId,
            estimate.EstimateNumber,
            estimate.FormatCode,
            estimate.TotalEstimatedAmount,
            estimate.CoverageAmount,
            estimate.PatientEstimatedAmount,
            estimate.GeneratedAtUtc,
            estimate.Status.ToString());
    }

    private Task EnsureAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        return tenantAccessService.EnsureCompanyRoleAsync(
            userId,
            cancellationToken,
            RoleNames.CompanyOwner,
            RoleNames.CompanyAdmin,
            RoleNames.CompanyManager);
    }

    private async Task<string> ResolveCountryCodeAsync(string? countryCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            return countryCode.Trim().ToUpperInvariant();
        }

        var settings = await dbContext.CompanySettings
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(settings?.CountryCode)
            ? "EE"
            : settings.CountryCode.Trim().ToUpperInvariant();
    }

    private static string BuildLegalText(string documentType, string countryCode, CostEstimate estimate)
    {
        if (documentType == "Kostenvoranschlag")
        {
            return $"Dokument: {documentType}\nLand: {countryCode}\nNummer: {estimate.EstimateNumber}\nGesamtkosten: {estimate.TotalEstimatedAmount:F2}\nDeckung: {estimate.CoverageAmount:F2}\nPatientenanteil: {estimate.PatientEstimatedAmount:F2}\nErstellt am: {estimate.GeneratedAtUtc:O}";
        }

        return $"Document: {documentType}\nCountry: {countryCode}\nEstimate Number: {estimate.EstimateNumber}\nTotal Amount: {estimate.TotalEstimatedAmount:F2}\nCoverage Amount: {estimate.CoverageAmount:F2}\nPatient Amount: {estimate.PatientEstimatedAmount:F2}\nGenerated At: {estimate.GeneratedAtUtc:O}";
    }
}
