using App.BLL.Contracts.CompanyUsers;
using App.BLL.Services;
using App.DAL.EF.Tenant;
using App.Domain;
using App.DTO.v1.CompanyUsers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{companySlug}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.CompanyOwner + "," + RoleNames.CompanyAdmin)]
public class CompanyUsersController(
    ICompanyUserService companyUserService,
    ITenantProvider tenantProvider)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CompanyUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CompanyUserResponse>>> List([FromRoute] string companySlug, CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var results = await companyUserService.ListAsync(User.UserId(), cancellationToken);
        return Ok(results.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(CompanyUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanyUserResponse>> Upsert(
        [FromRoute] string companySlug,
        [FromBody] UpsertCompanyUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TenantMatches(companySlug)) return Forbid();

        var result = await companyUserService.UpsertAsync(
            User.UserId(),
            new UpsertCompanyUserCommand(
                request.Email,
                request.RoleName,
                request.IsActive,
                request.TemporaryPassword),
            cancellationToken);

        return Ok(ToResponse(result));
    }

    private bool TenantMatches(string companySlug)
    {
        return string.Equals(tenantProvider.CompanySlug, companySlug, StringComparison.OrdinalIgnoreCase);
    }

    private static CompanyUserResponse ToResponse(CompanyUserResult result)
    {
        return new CompanyUserResponse
        {
            AppUserId = result.AppUserId,
            Email = result.Email,
            RoleName = result.RoleName,
            IsActive = result.IsActive,
            AssignedAtUtc = result.AssignedAtUtc
        };
    }
}
