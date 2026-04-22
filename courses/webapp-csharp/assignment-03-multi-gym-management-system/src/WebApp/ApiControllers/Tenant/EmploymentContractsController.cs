using App.BLL.Services;
using App.DTO.v1;
using App.DTO.v1.EmploymentContracts;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.ApiControllers.Tenant;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{gymCode}")]
public class EmploymentContractsController(IStaffWorkflowService staffWorkflowService) : ApiControllerBase
{
    [HttpGet("contracts")]
    public async Task<ActionResult<IReadOnlyCollection<ContractResponse>>> GetContracts(string gymCode)
    {
        return Ok(await staffWorkflowService.GetContractsAsync(gymCode));
    }

    [HttpPost("contracts")]
    public async Task<ActionResult<ContractResponse>> CreateContract(string gymCode, [FromBody] ContractUpsertRequest request)
    {
        return Ok(await staffWorkflowService.CreateContractAsync(gymCode, request));
    }

    [HttpPut("contracts/{id:guid}")]
    public async Task<ActionResult<ContractResponse>> UpdateContract(string gymCode, Guid id, [FromBody] ContractUpsertRequest request)
    {
        return Ok(await staffWorkflowService.UpdateContractAsync(gymCode, id, request));
    }

    [HttpDelete("contracts/{id:guid}")]
    public async Task<ActionResult<Message>> DeleteContract(string gymCode, Guid id)
    {
        await staffWorkflowService.DeleteContractAsync(gymCode, id);
        return Ok(new Message("Contract deleted."));
    }
}
