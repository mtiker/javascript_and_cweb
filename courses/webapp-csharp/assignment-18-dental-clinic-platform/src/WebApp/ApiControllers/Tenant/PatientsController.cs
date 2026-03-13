using App.BLL.Contracts.Patients;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.DTO.v1.Patients;
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
public class PatientsController(IPatientService patientService, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PatientResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PatientResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var patients = await patientService.ListAsync(User.UserId(), cancellationToken);
        return Ok(patients.Select(ToResponse).ToList());
    }

    [HttpGet("{patientId:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientResponse>> Get([FromRoute] string companySlug, [FromRoute] Guid patientId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var patient = await patientService.GetAsync(User.UserId(), patientId, cancellationToken);
        return Ok(ToResponse(patient));
    }

    [HttpGet("{patientId:guid}/profile")]
    [ProducesResponseType(typeof(PatientProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientProfileResponse>> GetProfile([FromRoute] string companySlug, [FromRoute] Guid patientId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var patient = await patientService.GetProfileAsync(User.UserId(), patientId, cancellationToken);
        return Ok(ToProfileResponse(patient));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PatientResponse>> Create([FromRoute] string companySlug, [FromBody] CreatePatientRequest request, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var patient = await patientService.CreateAsync(
            User.UserId(),
            new CreatePatientCommand(
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.PersonalCode,
                request.Email,
                request.Phone),
            cancellationToken);

        var response = ToResponse(patient);

        return CreatedAtAction(nameof(Get), new { version = "1", companySlug, patientId = response.Id }, response);
    }

    [HttpPut("{patientId:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientResponse>> Update(
        [FromRoute] string companySlug,
        [FromRoute] Guid patientId,
        [FromBody] UpdatePatientRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var patient = await patientService.UpdateAsync(
            User.UserId(),
            new UpdatePatientCommand(
                patientId,
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.PersonalCode,
                request.Email,
                request.Phone),
            cancellationToken);

        return Ok(ToResponse(patient));
    }

    [HttpDelete("{patientId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] string companySlug, [FromRoute] Guid patientId, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        await patientService.DeleteAsync(User.UserId(), patientId, cancellationToken);
        return NoContent();
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static PatientResponse ToResponse(PatientResult result)
    {
        return new PatientResponse
        {
            Id = result.Id,
            FirstName = result.FirstName,
            LastName = result.LastName,
            DateOfBirth = result.DateOfBirth,
            PersonalCode = result.PersonalCode,
            Email = result.Email,
            Phone = result.Phone
        };
    }

    private static PatientProfileResponse ToProfileResponse(PatientProfileResult result)
    {
        return new PatientProfileResponse
        {
            Id = result.Id,
            FirstName = result.FirstName,
            LastName = result.LastName,
            DateOfBirth = result.DateOfBirth,
            PersonalCode = result.PersonalCode,
            Email = result.Email,
            Phone = result.Phone,
            Teeth = result.Teeth.Select(tooth => new PatientToothResponse
            {
                Id = tooth.Id,
                ToothNumber = tooth.ToothNumber,
                Condition = tooth.Condition,
                Notes = tooth.Notes,
                StatusUpdatedAtUtc = tooth.StatusUpdatedAtUtc,
                LastTreatmentAtUtc = tooth.LastTreatmentAtUtc,
                LastTreatmentTypeName = tooth.LastTreatmentTypeName,
                LastTreatmentNotes = tooth.LastTreatmentNotes,
                History = tooth.History.Select(history => new PatientToothHistoryResponse
                {
                    Id = history.Id,
                    AppointmentId = history.AppointmentId,
                    TreatmentTypeName = history.TreatmentTypeName,
                    PerformedAtUtc = history.PerformedAtUtc,
                    Price = history.Price,
                    Notes = history.Notes
                }).ToArray()
            }).ToArray()
        };
    }
}
