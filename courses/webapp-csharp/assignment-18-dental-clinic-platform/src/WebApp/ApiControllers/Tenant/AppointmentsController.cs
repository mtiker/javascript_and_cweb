using App.BLL.Contracts.Appointments;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.DTO.v1.Appointments;
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
public class AppointmentsController(IAppointmentService appointmentService, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var appointments = await appointmentService.ListAsync(User.UserId(), cancellationToken);
        return Ok(appointments.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AppointmentResponse>> Create([FromRoute] string companySlug, [FromBody] CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var appointment = await appointmentService.CreateAsync(
            User.UserId(),
            new CreateAppointmentCommand(
                request.PatientId,
                request.DentistId,
                request.TreatmentRoomId,
                request.StartAtUtc,
                request.EndAtUtc,
                request.Notes),
            cancellationToken);

        return Created(string.Empty, ToResponse(appointment));
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static AppointmentResponse ToResponse(AppointmentResult result)
    {
        return new AppointmentResponse
        {
            Id = result.Id,
            PatientId = result.PatientId,
            DentistId = result.DentistId,
            TreatmentRoomId = result.TreatmentRoomId,
            StartAtUtc = result.StartAtUtc,
            EndAtUtc = result.EndAtUtc,
            Status = result.Status,
            Notes = result.Notes
        };
    }
}
