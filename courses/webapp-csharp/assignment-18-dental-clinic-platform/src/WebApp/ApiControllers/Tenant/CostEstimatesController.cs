using App.BLL.Contracts.Finance;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.DTO.v1;
using App.DTO.v1.CostEstimates;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager)]
public class CostEstimatesController(
    ICostEstimateService costEstimateService,
    ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CostEstimateResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CostEstimateResponse>>> List(
        [FromRoute] string companySlug,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var estimates = await costEstimateService.ListAsync(User.UserId(), patientId, cancellationToken);
        return Ok(estimates.Select(ToResponse).ToList());
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

        var estimate = await costEstimateService.CreateAsync(
            User.UserId(),
            new CreateCostEstimateCommand(
                request.PatientId,
                request.TreatmentPlanId,
                request.PatientInsurancePolicyId,
                request.EstimateNumber,
                request.FormatCode),
            cancellationToken);

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

        var legalEstimate = await costEstimateService.GetLegalAsync(User.UserId(), costEstimateId, countryCode, cancellationToken);

        return Ok(new LegalEstimateResponse
        {
            CostEstimateId = legalEstimate.CostEstimateId,
            CountryCode = legalEstimate.CountryCode,
            DocumentType = legalEstimate.DocumentType,
            GeneratedText = legalEstimate.GeneratedText,
            GeneratedAtUtc = legalEstimate.GeneratedAtUtc
        });
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static CostEstimateResponse ToResponse(CostEstimateResult estimate)
    {
        return new CostEstimateResponse
        {
            Id = estimate.Id,
            PatientId = estimate.PatientId,
            TreatmentPlanId = estimate.TreatmentPlanId,
            PatientInsurancePolicyId = estimate.PatientInsurancePolicyId,
            InsurancePlanId = estimate.InsurancePlanId,
            EstimateNumber = estimate.EstimateNumber,
            FormatCode = estimate.FormatCode,
            TotalEstimatedAmount = estimate.TotalEstimatedAmount,
            CoverageAmount = estimate.CoverageAmount,
            PatientEstimatedAmount = estimate.PatientEstimatedAmount,
            GeneratedAtUtc = estimate.GeneratedAtUtc,
            Status = estimate.Status
        };
    }
}
