using App.BLL.Contracts.Appointments;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Enums;
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

    [HttpPost("{appointmentId:guid}/clinical-record")]
    [ProducesResponseType(typeof(AppointmentClinicalRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(App.DTO.v1.Message), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppointmentClinicalRecordResponse>> RecordClinicalWork(
        [FromRoute] string companySlug,
        [FromRoute] Guid appointmentId,
        [FromBody] RecordAppointmentClinicalRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var items = new List<RecordAppointmentClinicalItemCommand>(request.Items.Count);

        foreach (var item in request.Items)
        {
            if (!Enum.TryParse<ToothConditionStatus>(item.Condition, true, out var condition))
            {
                return BadRequest(new App.DTO.v1.Message($"Invalid tooth condition value '{item.Condition}'."));
            }

            if (!ToothChart.IsValidPermanentToothNumber(item.ToothNumber))
            {
                return BadRequest(new App.DTO.v1.Message($"Tooth number {item.ToothNumber} is not a valid permanent FDI tooth number."));
            }

            items.Add(new RecordAppointmentClinicalItemCommand(
                item.TreatmentTypeId,
                item.PlanItemId,
                item.ToothNumber,
                condition,
                item.Price,
                item.Notes));
        }

        var result = await appointmentService.RecordClinicalWorkAsync(
            User.UserId(),
            new RecordAppointmentClinicalCommand(
                appointmentId,
                request.PerformedAtUtc,
                request.MarkAppointmentCompleted,
                items),
            cancellationToken);

        return Ok(new AppointmentClinicalRecordResponse
        {
            AppointmentId = result.AppointmentId,
            Status = result.Status,
            RecordedItemCount = result.RecordedItemCount
        });
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
