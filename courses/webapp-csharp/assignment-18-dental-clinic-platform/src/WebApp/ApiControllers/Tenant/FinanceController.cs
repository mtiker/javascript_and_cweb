using App.BLL.Contracts.Finance;
using App.BLL.Contracts.TreatmentPlans;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.DTO.v1.Finance;
using App.DTO.v1.InsurancePlans;
using App.DTO.v1.Invoices;
using App.DTO.v1.PatientInsurancePolicies;
using App.DTO.v1.Patients;
using App.DTO.v1.TreatmentPlans;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin + "," + RoleNames.CompanyManager + "," + RoleNames.CompanyEmployee)]
public class FinanceController(IFinanceWorkspaceService financeWorkspaceService, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet("workspace/{patientId:guid}")]
    [ProducesResponseType(typeof(FinanceWorkspaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(App.DTO.v1.Message), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinanceWorkspaceResponse>> Workspace(
        [FromRoute] string companySlug,
        [FromRoute] Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var workspace = await financeWorkspaceService.GetWorkspaceAsync(User.UserId(), patientId, cancellationToken);

        return Ok(new FinanceWorkspaceResponse
        {
            Patient = new PatientResponse
            {
                Id = workspace.Patient.Id,
                FirstName = workspace.Patient.FirstName,
                LastName = workspace.Patient.LastName,
                DateOfBirth = workspace.Patient.DateOfBirth,
                PersonalCode = workspace.Patient.PersonalCode,
                Email = workspace.Patient.Email,
                Phone = workspace.Patient.Phone
            },
            InsurancePlans = workspace.InsurancePlans.Select(entity => new InsurancePlanResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                CountryCode = entity.CountryCode,
                CoverageType = entity.CoverageType,
                IsActivePlan = entity.IsActivePlan,
                ClaimSubmissionEndpoint = entity.ClaimSubmissionEndpoint
            }).ToArray(),
            Plans = workspace.Plans.Select(ToTreatmentPlanResponse).ToArray(),
            Policies = workspace.Policies.Select(entity => new PatientInsurancePolicyResponse
            {
                Id = entity.Id,
                PatientId = entity.PatientId,
                InsurancePlanId = entity.InsurancePlanId,
                InsurancePlanName = entity.InsurancePlanName,
                PolicyNumber = entity.PolicyNumber,
                MemberNumber = entity.MemberNumber,
                GroupNumber = entity.GroupNumber,
                CoverageStart = entity.CoverageStart,
                CoverageEnd = entity.CoverageEnd,
                AnnualMaximum = entity.AnnualMaximum,
                Deductible = entity.Deductible,
                CoveragePercent = entity.CoveragePercent,
                Status = entity.Status
            }).ToArray(),
            Estimates = workspace.Estimates.Select(entity => new App.DTO.v1.CostEstimates.CostEstimateResponse
            {
                Id = entity.Id,
                PatientId = entity.PatientId,
                TreatmentPlanId = entity.TreatmentPlanId,
                PatientInsurancePolicyId = entity.PatientInsurancePolicyId,
                InsurancePlanId = entity.InsurancePlanId,
                EstimateNumber = entity.EstimateNumber,
                FormatCode = entity.FormatCode,
                TotalEstimatedAmount = entity.TotalEstimatedAmount,
                CoverageAmount = entity.CoverageAmount,
                PatientEstimatedAmount = entity.PatientEstimatedAmount,
                GeneratedAtUtc = entity.GeneratedAtUtc,
                Status = entity.Status
            }).ToArray(),
            Procedures = workspace.Procedures.Select(entity => new PerformedProcedureResponse
            {
                Id = entity.Id,
                PatientId = entity.PatientId,
                TreatmentTypeId = entity.TreatmentTypeId,
                PlanItemId = entity.PlanItemId,
                AppointmentId = entity.AppointmentId,
                ToothNumber = entity.ToothNumber,
                PerformedAtUtc = entity.PerformedAtUtc,
                Price = entity.Price,
                TreatmentTypeName = entity.TreatmentTypeName,
                Notes = entity.Notes
            }).ToArray(),
            Invoices = workspace.Invoices.Select(ToInvoiceSummaryResponse).ToArray()
        });
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static TreatmentPlanResponse ToTreatmentPlanResponse(TreatmentPlanResult entity)
    {
        return new TreatmentPlanResponse
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            DentistId = entity.DentistId,
            Status = entity.Status,
            SubmittedAtUtc = entity.SubmittedAtUtc,
            ApprovedAtUtc = entity.ApprovedAtUtc,
            IsLocked = entity.IsLocked,
            Items = entity.Items.Select(item => new TreatmentPlanItemResponse
            {
                Id = item.Id,
                TreatmentTypeId = item.TreatmentTypeId,
                TreatmentTypeName = item.TreatmentTypeName,
                Sequence = item.Sequence,
                Urgency = item.Urgency,
                EstimatedPrice = item.EstimatedPrice,
                Decision = item.Decision,
                DecisionAtUtc = item.DecisionAtUtc,
                DecisionNotes = item.DecisionNotes
            }).ToArray()
        };
    }

    private static InvoiceResponse ToInvoiceSummaryResponse(InvoiceSummaryResult entity)
    {
        return new InvoiceResponse
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            CostEstimateId = entity.CostEstimateId,
            InvoiceNumber = entity.InvoiceNumber,
            TotalAmount = entity.TotalAmount,
            CoverageAmount = entity.CoverageAmount,
            PatientResponsibilityAmount = entity.PatientResponsibilityAmount,
            PaidAmount = entity.PaidAmount,
            BalanceAmount = entity.BalanceAmount,
            DueDateUtc = entity.DueDateUtc,
            Status = entity.Status
        };
    }
}
