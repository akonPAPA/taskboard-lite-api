using Microsoft.EntityFrameworkCore;
using TaskBoardLite.Api.Contracts;
using TaskBoardLite.Api.Errors;
using TaskBoardLite.Api.Mapping;
using TaskBoardLite.Domain.Entities;
using TaskBoardLite.Infrastructure.Persistence;

namespace TaskBoardLite.Api.Services;

public sealed class ProjectService
{
    private readonly TaskBoardLiteDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ProjectService(TaskBoardLiteDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code!.Trim().ToUpperInvariant();
        var codeExists = await _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Code == normalizedCode, cancellationToken);

        if (codeExists)
        {
            throw new DuplicateValueException("Project code already exists.");
        }

        var project = new Project(request.Name!, request.Code!, request.Description, _timeProvider.GetUtcNow());
        _dbContext.Projects.Add(project);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new DuplicateValueException("Project code already exists.");
        }

        return project.ToResponse();
    }

    public async Task<IReadOnlyList<ProjectResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .OrderByDescending(project => project.CreatedAtUtc)
            .Select(project => new ProjectResponse(
                project.Id,
                project.Name,
                project.Code,
                project.Description,
                project.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == id)
            .Select(project => new ProjectResponse(
                project.Id,
                project.Name,
                project.Code,
                project.Description,
                project.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return project ?? throw new NotFoundException("Project was not found.");
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException("Project was not found.");
        }

        var hasWorkItems = await _dbContext.WorkItems.AnyAsync(workItem => workItem.ProjectId == id, cancellationToken);
        if (hasWorkItems)
        {
            throw new ConflictException("Project cannot be deleted while it contains work items.");
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

