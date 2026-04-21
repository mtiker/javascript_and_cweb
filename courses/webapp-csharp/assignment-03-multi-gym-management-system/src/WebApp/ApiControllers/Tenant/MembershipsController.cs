using App.BLL.Contracts;
using App.DTO.v1;
using App.DTO.v1.Tenant;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class MembershipsController(IMembershipWorkflowService membershipWorkflowService) : ApiControllerBase
{
    [HttpGet("membership-packages")]
    public async Task<ActionResult<IReadOnlyCollection<MembershipPackageResponse>>> GetPackages(string gymCode)
    {
        return Ok(await membershipWorkflowService.GetPackagesAsync(gymCode));
    }

    [HttpPost("membership-packages")]
    public async Task<ActionResult<MembershipPackageResponse>> CreatePackage(string gymCode, [FromBody] MembershipPackageUpsertRequest request)
    {
        return Ok(await membershipWorkflowService.CreatePackageAsync(gymCode, request));
    }

    [HttpPut("membership-packages/{id:guid}")]
    public async Task<ActionResult<MembershipPackageResponse>> UpdatePackage(string gymCode, Guid id, [FromBody] MembershipPackageUpsertRequest request)
    {
        return Ok(await membershipWorkflowService.UpdatePackageAsync(gymCode, id, request));
    }

    [HttpDelete("membership-packages/{id:guid}")]
    public async Task<ActionResult<Message>> DeletePackage(string gymCode, Guid id)
    {
        await membershipWorkflowService.DeletePackageAsync(gymCode, id);
        return Ok(new Message("Membership package deleted."));
    }

    [HttpGet("memberships")]
    public async Task<ActionResult<IReadOnlyCollection<MembershipResponse>>> GetMemberships(string gymCode)
    {
        return Ok(await membershipWorkflowService.GetMembershipsAsync(gymCode));
    }

    [HttpPost("memberships")]
    public async Task<ActionResult<MembershipSaleResponse>> SellMembership(string gymCode, [FromBody] SellMembershipRequest request)
    {
        return Ok(await membershipWorkflowService.SellMembershipAsync(gymCode, request));
    }

    [HttpDelete("memberships/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteMembership(string gymCode, Guid id)
    {
        await membershipWorkflowService.DeleteMembershipAsync(gymCode, id);
        return Ok(new Message("Membership deleted."));
    }

    [HttpGet("payments")]
    public async Task<ActionResult<IReadOnlyCollection<PaymentResponse>>> GetPayments(string gymCode)
    {
        return Ok(await membershipWorkflowService.GetPaymentsAsync(gymCode));
    }

    [HttpPost("payments")]
    public async Task<ActionResult<PaymentResponse>> CreatePayment(string gymCode, [FromBody] PaymentCreateRequest request)
    {
        return Ok(await membershipWorkflowService.CreatePaymentAsync(gymCode, request));
    }
}
