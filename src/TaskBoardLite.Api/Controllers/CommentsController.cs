using Microsoft.AspNetCore.Mvc;
using TaskBoardLite.Api.Contracts;
using TaskBoardLite.Api.Services;

namespace TaskBoardLite.Api.Controllers;

[ApiController]
public sealed class CommentsController : ControllerBase
{
    private readonly CommentService _commentService;

    public CommentsController(CommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost("api/work-items/{workItemId:int}/comments")]
    [ProducesResponseType<CommentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CommentResponse>> Create(
        int workItemId,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await _commentService.CreateAsync(workItemId, request, cancellationToken);
        return CreatedAtAction(nameof(List), new { workItemId = comment.WorkItemId }, comment);
    }

    [HttpGet("api/work-items/{workItemId:int}/comments")]
    [ProducesResponseType<IReadOnlyList<CommentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CommentResponse>>> List(
        int workItemId,
        CancellationToken cancellationToken)
    {
        var comments = await _commentService.ListAsync(workItemId, cancellationToken);
        return Ok(comments);
    }
}
