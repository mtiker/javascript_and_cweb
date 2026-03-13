using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain.Enums;
using App.Domain;
using App.Domain.Entities;
using App.BLL.Services;
using App.DTO.v1;
using App.DTO.v1.CostEstimates;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
public class CostEstimatesController(
    AppDbContext dbContext,
    ITenantProvider tenantProvider,
    ISubscriptionPolicyService subscriptionPolicyService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CostEstimateResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CostEstimateResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        await subscriptionPolicyService.EnsureTierAtLeastAsync("CostEstimates", SubscriptionTier.Standard, cancellationToken);

        var estimates = await dbContext.CostEstimates
            .AsNoTracking()
            .OrderByDescending(entity => entity.GeneratedAtUtc)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);

        return Ok(estimates);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CostEstimateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CostEstimateResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreateCostEstimateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        await subscriptionPolicyService.EnsureTierAtLeastAsync("CostEstimates", SubscriptionTier.Standard, cancellationToken);

        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            return BadRequest(new Message("Patient does not exist in tenant."));
        }

        var planExists = await dbContext.TreatmentPlans
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == request.TreatmentPlanId, cancellationToken);
        if (!planExists)
        {
            return BadRequest(new Message("Treatment plan does not exist in tenant."));
        }

        if (request.InsurancePlanId.HasValue)
        {
            var insuranceExists = await dbContext.InsurancePlans
                .AsNoTracking()
                .AnyAsync(entity => entity.Id == request.InsurancePlanId.Value, cancellationToken);
            if (!insuranceExists)
            {
                return BadRequest(new Message("Insurance plan does not exist in tenant."));
            }
        }

        var estimateNumber = request.EstimateNumber.Trim();
        var duplicateNumber = await dbContext.CostEstimates
            .AsNoTracking()
            .AnyAsync(entity => entity.EstimateNumber == estimateNumber, cancellationToken);
        if (duplicateNumber)
        {
            return BadRequest(new Message("Estimate number already exists."));
        }

        var estimate = new CostEstimate
        {
            PatientId = request.PatientId,
            TreatmentPlanId = request.TreatmentPlanId,
            InsurancePlanId = request.InsurancePlanId,
            EstimateNumber = estimateNumber,
            FormatCode = request.FormatCode.Trim().ToUpperInvariant(),
            TotalEstimatedAmount = request.TotalEstimatedAmount,
            GeneratedAtUtc = DateTime.UtcNow,
            Status = "Draft"
        };

        dbContext.CostEstimates.Add(estimate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created(string.Empty, ToResponse(estimate));
    }

    [HttpGet("{costEstimateId:guid}/legal")]
    [ProducesResponseType(typeof(LegalEstimateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LegalEstimateResponse>> Legal(
        [FromRoute] string companySlug,
        [FromRoute] Guid costEstimateId,
        [FromQuery] string? countryCode,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();
        await subscriptionPolicyService.EnsureTierAtLeastAsync("CostEstimatesLegalOutput", SubscriptionTier.Standard, cancellationToken);

        var estimate = await dbContext.CostEstimates
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == costEstimateId, cancellationToken);
        if (estimate == null)
        {
            return NotFound(new Message("Cost estimate not found."));
        }

        var effectiveCountryCode = await ResolveCountryCodeAsync(countryCode, cancellationToken);
        var documentType = string.Equals(effectiveCountryCode, "DE", StringComparison.OrdinalIgnoreCase)
            ? "Kostenvoranschlag"
            : "Cost Estimate";

        var generatedText = BuildLegalText(documentType, effectiveCountryCode, estimate);
        var response = new LegalEstimateResponse
        {
            CostEstimateId = estimate.Id,
            CountryCode = effectiveCountryCode,
            DocumentType = documentType,
            GeneratedText = generatedText,
            GeneratedAtUtc = DateTime.UtcNow
        };

        return Ok(response);
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
            return $"Dokument: {documentType}\nLand: {countryCode}\nNummer: {estimate.EstimateNumber}\nGesamtkosten: {estimate.TotalEstimatedAmount:F2}\nErstellt am: {estimate.GeneratedAtUtc:O}";
        }

        return $"Document: {documentType}\nCountry: {countryCode}\nEstimate Number: {estimate.EstimateNumber}\nTotal Amount: {estimate.TotalEstimatedAmount:F2}\nGenerated At: {estimate.GeneratedAtUtc:O}";
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static CostEstimateResponse ToResponse(CostEstimate estimate)
    {
        return new CostEstimateResponse
        {
            Id = estimate.Id,
            PatientId = estimate.PatientId,
            TreatmentPlanId = estimate.TreatmentPlanId,
            InsurancePlanId = estimate.InsurancePlanId,
            EstimateNumber = estimate.EstimateNumber,
            FormatCode = estimate.FormatCode,
            TotalEstimatedAmount = estimate.TotalEstimatedAmount,
            GeneratedAtUtc = estimate.GeneratedAtUtc,
            Status = estimate.Status
        };
    }
}
