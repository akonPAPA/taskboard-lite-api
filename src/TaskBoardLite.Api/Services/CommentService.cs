using Microsoft.EntityFrameworkCore;
using TaskBoardLite.Api.Contracts;
using TaskBoardLite.Api.Errors;
using TaskBoardLite.Api.Mapping;
using TaskBoardLite.Domain.Entities;
using TaskBoardLite.Infrastructure.Persistence;

namespace TaskBoardLite.Api.Services;

public sealed class CommentService
{
    private readonly TaskBoardLiteDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CommentService(TaskBoardLiteDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<CommentResponse> CreateAsync(
        int workItemId,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var workItemExists = await _dbContext.WorkItems.AnyAsync(workItem => workItem.Id == workItemId, cancellationToken);
        if (!workItemExists)
        {
            throw new NotFoundException("Work item was not found.");
        }

        var comment = new WorkItemComment(workItemId, request.AuthorName!, request.Body!, _timeProvider.GetUtcNow());
        _dbContext.WorkItemComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return comment.ToResponse();
    }

    public async Task<IReadOnlyList<CommentResponse>> ListAsync(int workItemId, CancellationToken cancellationToken)
    {
        var workItemExists = await _dbContext.WorkItems.AnyAsync(workItem => workItem.Id == workItemId, cancellationToken);
        if (!workItemExists)
        {
            throw new NotFoundException("Work item was not found.");
        }

        return await _dbContext.WorkItemComments
            .AsNoTracking()
            .Where(comment => comment.WorkItemId == workItemId)
            .OrderBy(comment => comment.CreatedAtUtc)
            .Select(comment => new CommentResponse(
                comment.Id,
                comment.WorkItemId,
                comment.AuthorName,
                comment.Body,
                comment.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
