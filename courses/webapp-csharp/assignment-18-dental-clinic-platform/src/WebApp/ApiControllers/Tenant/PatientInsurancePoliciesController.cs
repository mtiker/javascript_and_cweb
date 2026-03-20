using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.PatientInsurancePolicies;
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
public class PatientInsurancePoliciesController(AppDbContext dbContext, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PatientInsurancePolicyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PatientInsurancePolicyResponse>>> List(
        [FromRoute] string companySlug,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var query = dbContext.PatientInsurancePolicies
            .AsNoTracking()
            .Include(entity => entity.InsurancePlan)
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(entity => entity.PatientId == patientId.Value);
        }

        var policies = await query
            .OrderByDescending(entity => entity.CoverageStart)
            .ToListAsync(cancellationToken);

        return Ok(policies.Select(ToResponse).ToList());
    }

    [HttpGet("{policyId:guid}")]
    [ProducesResponseType(typeof(PatientInsurancePolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientInsurancePolicyResponse>> GetById(
        [FromRoute] string companySlug,
        [FromRoute] Guid policyId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var policy = await dbContext.PatientInsurancePolicies
            .AsNoTracking()
            .Include(entity => entity.InsurancePlan)
            .SingleOrDefaultAsync(entity => entity.Id == policyId, cancellationToken);
        if (policy == null)
        {
            return NotFound(new Message("Patient insurance policy not found."));
        }

        return Ok(ToResponse(policy));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PatientInsurancePolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PatientInsurancePolicyResponse>> Create(
        [FromRoute] string companySlug,
        [FromBody] CreatePatientInsurancePolicyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var validationError = await ValidateRequestAsync(
            request.PatientId,
            request.InsurancePlanId,
            request.PolicyNumber,
            request.CoverageStart,
            request.CoverageEnd,
            null,
            cancellationToken);
        if (validationError != null)
        {
            return BadRequest(new Message(validationError));
        }

        if (!Enum.TryParse<PatientInsurancePolicyStatus>(request.Status, true, out var status))
        {
            return BadRequest(new Message("Invalid patient insurance policy status."));
        }

        var policy = new PatientInsurancePolicy
        {
            PatientId = request.PatientId,
            InsurancePlanId = request.InsurancePlanId,
            PolicyNumber = request.PolicyNumber.Trim(),
            MemberNumber = NormalizeOptional(request.MemberNumber),
            GroupNumber = NormalizeOptional(request.GroupNumber),
            CoverageStart = request.CoverageStart,
            CoverageEnd = request.CoverageEnd,
            AnnualMaximum = request.AnnualMaximum,
            Deductible = request.Deductible,
            CoveragePercent = request.CoveragePercent,
            Status = status
        };

        dbContext.PatientInsurancePolicies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.PatientInsurancePolicies
            .AsNoTracking()
            .Include(entity => entity.InsurancePlan)
            .SingleAsync(entity => entity.Id == policy.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { version = "1", companySlug, policyId = policy.Id }, ToResponse(saved));
    }

    [HttpPut("{policyId:guid}")]
    [ProducesResponseType(typeof(PatientInsurancePolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PatientInsurancePolicyResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid policyId,
        [FromBody] UpdatePatientInsurancePolicyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var policy = await dbContext.PatientInsurancePolicies
            .SingleOrDefaultAsync(entity => entity.Id == policyId, cancellationToken);
        if (policy == null)
        {
            return NotFound(new Message("Patient insurance policy not found."));
        }

        var validationError = await ValidateRequestAsync(
            policy.PatientId,
            request.InsurancePlanId,
            request.PolicyNumber,
            request.CoverageStart,
            request.CoverageEnd,
            policyId,
            cancellationToken);
        if (validationError != null)
        {
            return BadRequest(new Message(validationError));
        }

        if (!Enum.TryParse<PatientInsurancePolicyStatus>(request.Status, true, out var status))
        {
            return BadRequest(new Message("Invalid patient insurance policy status."));
        }

        policy.InsurancePlanId = request.InsurancePlanId;
        policy.PolicyNumber = request.PolicyNumber.Trim();
        policy.MemberNumber = NormalizeOptional(request.MemberNumber);
        policy.GroupNumber = NormalizeOptional(request.GroupNumber);
        policy.CoverageStart = request.CoverageStart;
        policy.CoverageEnd = request.CoverageEnd;
        policy.AnnualMaximum = request.AnnualMaximum;
        policy.Deductible = request.Deductible;
        policy.CoveragePercent = request.CoveragePercent;
        policy.Status = status;

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.PatientInsurancePolicies
            .AsNoTracking()
            .Include(entity => entity.InsurancePlan)
            .SingleAsync(entity => entity.Id == policy.Id, cancellationToken);

        return Ok(ToResponse(saved));
    }

    [HttpDelete("{policyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid policyId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var policy = await dbContext.PatientInsurancePolicies
            .SingleOrDefaultAsync(entity => entity.Id == policyId, cancellationToken);
        if (policy == null)
        {
            return NotFound(new Message("Patient insurance policy not found."));
        }

        dbContext.PatientInsurancePolicies.Remove(policy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateRequestAsync(
        Guid patientId,
        Guid insurancePlanId,
        string policyNumber,
        DateOnly coverageStart,
        DateOnly? coverageEnd,
        Guid? currentPolicyId,
        CancellationToken cancellationToken)
    {
        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == patientId, cancellationToken);
        if (!patientExists)
        {
            return "Patient does not exist in tenant.";
        }

        var insurancePlanExists = await dbContext.InsurancePlans
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == insurancePlanId, cancellationToken);
        if (!insurancePlanExists)
        {
            return "Insurance plan does not exist in tenant.";
        }

        if (coverageEnd.HasValue && coverageEnd.Value < coverageStart)
        {
            return "Coverage end date cannot be earlier than coverage start date.";
        }

        var normalizedPolicyNumber = policyNumber.Trim();
        var duplicatePolicy = await dbContext.PatientInsurancePolicies
            .AsNoTracking()
            .AnyAsync(
                entity => entity.PolicyNumber == normalizedPolicyNumber &&
                          (!currentPolicyId.HasValue || entity.Id != currentPolicyId.Value),
                cancellationToken);
        if (duplicatePolicy)
        {
            return "Policy number already exists in tenant.";
        }

        return null;
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static PatientInsurancePolicyResponse ToResponse(PatientInsurancePolicy entity)
    {
        return new PatientInsurancePolicyResponse
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            InsurancePlanId = entity.InsurancePlanId,
            InsurancePlanName = entity.InsurancePlan?.Name ?? "-",
            PolicyNumber = entity.PolicyNumber,
            MemberNumber = entity.MemberNumber,
            GroupNumber = entity.GroupNumber,
            CoverageStart = entity.CoverageStart,
            CoverageEnd = entity.CoverageEnd,
            AnnualMaximum = entity.AnnualMaximum,
            Deductible = entity.Deductible,
            CoveragePercent = entity.CoveragePercent,
            Status = entity.Status.ToString()
        };
    }
}
