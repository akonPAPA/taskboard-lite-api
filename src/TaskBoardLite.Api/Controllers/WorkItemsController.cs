using Microsoft.AspNetCore.Mvc;
using TaskBoardLite.Api.Contracts;
using TaskBoardLite.Api.Services;

namespace TaskBoardLite.Api.Controllers;

[ApiController]
public sealed class WorkItemsController : ControllerBase
{
    private readonly WorkItemService _workItemService;

    public WorkItemsController(WorkItemService workItemService)
    {
        _workItemService = workItemService;
    }

    [HttpPost("api/projects/{projectId:int}/work-items")]
    [ProducesResponseType<WorkItemResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkItemResponse>> Create(
        int projectId,
        CreateWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await _workItemService.CreateAsync(projectId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = workItem.Id }, workItem);
    }

    [HttpGet("api/projects/{projectId:int}/work-items")]
    [ProducesResponseType<PagedResponse<WorkItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<WorkItemResponse>>> List(
        int projectId,
        [FromQuery] WorkItemQueryParameters query,
        CancellationToken cancellationToken)
    {
        var workItems = await _workItemService.ListAsync(projectId, query, cancellationToken);
        return Ok(workItems);
    }

    [HttpGet("api/work-items/{id:int}")]
    [ProducesResponseType<WorkItemResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkItemResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var workItem = await _workItemService.GetAsync(id, cancellationToken);
        return Ok(workItem);
    }

    [HttpPut("api/work-items/{id:int}")]
    [ProducesResponseType<WorkItemResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkItemResponse>> Update(
        int id,
        UpdateWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await _workItemService.UpdateAsync(id, request, cancellationToken);
        return Ok(workItem);
    }

    [HttpPatch("api/work-items/{id:int}/status")]
    [ProducesResponseType<WorkItemResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkItemResponse>> ChangeStatus(
        int id,
        ChangeWorkItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await _workItemService.ChangeStatusAsync(id, request, cancellationToken);
        return Ok(workItem);
    }

    [HttpDelete("api/work-items/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _workItemService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
