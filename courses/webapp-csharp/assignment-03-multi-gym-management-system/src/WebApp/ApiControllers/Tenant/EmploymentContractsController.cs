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
    [ProducesResponseType(typeof(IReadOnlyCollection<ContractResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ContractResponse>>> GetContracts(string gymCode, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.GetContractsAsync(gymCode, cancellationToken));
    }

    [HttpPost("contracts")]
    [ProducesResponseType(typeof(ContractResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContractResponse>> CreateContract(string gymCode, [FromBody] ContractUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.CreateContractAsync(gymCode, request, cancellationToken));
    }

    [HttpPut("contracts/{id:guid}")]
    [ProducesResponseType(typeof(ContractResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContractResponse>> UpdateContract(string gymCode, Guid id, [FromBody] ContractUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await staffWorkflowService.UpdateContractAsync(gymCode, id, request, cancellationToken));
    }

    [HttpDelete("contracts/{id:guid}")]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    public async Task<ActionResult<Message>> DeleteContract(string gymCode, Guid id, CancellationToken cancellationToken)
    {
        await staffWorkflowService.DeleteContractAsync(gymCode, id, cancellationToken);
        return Ok(new Message("Contract deleted."));
    }
}
