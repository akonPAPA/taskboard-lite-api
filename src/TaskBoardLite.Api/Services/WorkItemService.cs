using Microsoft.EntityFrameworkCore;
using TaskBoardLite.Api.Contracts;
using TaskBoardLite.Api.Errors;
using TaskBoardLite.Api.Mapping;
using TaskBoardLite.Domain.Entities;
using TaskBoardLite.Infrastructure.Persistence;

namespace TaskBoardLite.Api.Services;

public sealed class WorkItemService
{
    private readonly TaskBoardLiteDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public WorkItemService(TaskBoardLiteDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<WorkItemResponse> CreateAsync(
        int projectId,
        CreateWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var projectExists = await _dbContext.Projects.AnyAsync(project => project.Id == projectId, cancellationToken);
        if (!projectExists)
        {
            throw new NotFoundException("Project was not found.");
        }

        var workItem = new WorkItem(
            projectId,
            request.Title!,
            request.Description,
            request.Priority,
            request.DueDateUtc,
            _timeProvider.GetUtcNow());

        _dbContext.WorkItems.Add(workItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return workItem.ToResponse();
    }

    public async Task<PagedResponse<WorkItemResponse>> ListAsync(
        int projectId,
        WorkItemQueryParameters query,
        CancellationToken cancellationToken)
    {
        var projectExists = await _dbContext.Projects.AnyAsync(project => project.Id == projectId, cancellationToken);
        if (!projectExists)
        {
            throw new NotFoundException("Project was not found.");
        }

        var workItems = _dbContext.WorkItems
            .AsNoTracking()
            .Where(workItem => workItem.ProjectId == projectId);

        if (query.Status is not null)
        {
            workItems = workItems.Where(workItem => workItem.Status == query.Status);
        }

        if (query.Priority is not null)
        {
            workItems = workItems.Where(workItem => workItem.Priority == query.Priority);
        }

        if (query.DueBeforeUtc is not null)
        {
            workItems = workItems.Where(workItem => workItem.DueDateUtc != null && workItem.DueDateUtc <= query.DueBeforeUtc);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            workItems = workItems.Where(workItem => workItem.Title.ToLower().Contains(term));
        }

        workItems = ApplySorting(workItems, query.SortBy, query.SortDirection);

        var totalRecords = await workItems.CountAsync(cancellationToken);
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)query.PageSize);

        var items = await workItems
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(workItem => new WorkItemResponse(
                workItem.Id,
                workItem.ProjectId,
                workItem.Title,
                workItem.Description,
                workItem.Status,
                workItem.Priority,
                workItem.DueDateUtc,
                workItem.CreatedAtUtc,
                workItem.UpdatedAtUtc,
                workItem.Version))
            .ToListAsync(cancellationToken);

        return new PagedResponse<WorkItemResponse>(items, query.Page, query.PageSize, totalRecords, totalPages);
    }

    public async Task<WorkItemResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var workItem = await _dbContext.WorkItems
            .AsNoTracking()
            .Where(workItem => workItem.Id == id)
            .Select(workItem => new WorkItemResponse(
                workItem.Id,
                workItem.ProjectId,
                workItem.Title,
                workItem.Description,
                workItem.Status,
                workItem.Priority,
                workItem.DueDateUtc,
                workItem.CreatedAtUtc,
                workItem.UpdatedAtUtc,
                workItem.Version))
            .SingleOrDefaultAsync(cancellationToken);

        return workItem ?? throw new NotFoundException("Work item was not found.");
    }

    public async Task<WorkItemResponse> UpdateAsync(
        int id,
        UpdateWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await _dbContext.WorkItems.SingleOrDefaultAsync(workItem => workItem.Id == id, cancellationToken);
        if (workItem is null)
        {
            throw new NotFoundException("Work item was not found.");
        }

        EnsureVersionMatches(workItem.Version, request.Version);
        _dbContext.Entry(workItem).Property(item => item.Version).OriginalValue = request.Version;

        workItem.UpdateDetails(
            request.Title!,
            request.Description,
            request.Priority,
            request.DueDateUtc,
            _timeProvider.GetUtcNow());

        await SaveWithConcurrencyHandlingAsync(cancellationToken);
        return workItem.ToResponse();
    }

    public async Task<WorkItemResponse> ChangeStatusAsync(
        int id,
        ChangeWorkItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await _dbContext.WorkItems.SingleOrDefaultAsync(workItem => workItem.Id == id, cancellationToken);
        if (workItem is null)
        {
            throw new NotFoundException("Work item was not found.");
        }

        EnsureVersionMatches(workItem.Version, request.Version);
        _dbContext.Entry(workItem).Property(item => item.Version).OriginalValue = request.Version;

        workItem.ChangeStatus(request.Status, _timeProvider.GetUtcNow());

        await SaveWithConcurrencyHandlingAsync(cancellationToken);
        return workItem.ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var workItem = await _dbContext.WorkItems.SingleOrDefaultAsync(workItem => workItem.Id == id, cancellationToken);
        if (workItem is null)
        {
            throw new NotFoundException("Work item was not found.");
        }

        _dbContext.WorkItems.Remove(workItem);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public static IQueryable<WorkItem> ApplySorting(IQueryable<WorkItem> workItems, string sortBy, string sortDirection)
    {
        var ascending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "duedate" => ascending
                ? workItems.OrderBy(workItem => workItem.DueDateUtc == null).ThenBy(workItem => workItem.DueDateUtc)
                : workItems.OrderBy(workItem => workItem.DueDateUtc == null).ThenByDescending(workItem => workItem.DueDateUtc),
            _ => ascending
                ? workItems.OrderBy(workItem => workItem.CreatedAtUtc)
                : workItems.OrderByDescending(workItem => workItem.CreatedAtUtc)
        };
    }

    private static void EnsureVersionMatches(int storedVersion, int requestedVersion)
    {
        if (storedVersion != requestedVersion)
        {
            throw new OptimisticConcurrencyException("The supplied version is stale. Read the work item again and retry with the current version.");
        }
    }

    private async Task SaveWithConcurrencyHandlingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new OptimisticConcurrencyException("The work item was changed by another request. Read it again and retry with the current version.");
        }
    }
}

