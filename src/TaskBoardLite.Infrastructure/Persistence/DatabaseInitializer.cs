using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskBoardLite.Domain.Entities;
using TaskBoardLite.Domain.Enums;

namespace TaskBoardLite.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly TaskBoardLiteDbContext _dbContext;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly TimeProvider _timeProvider;

    public DatabaseInitializer(
        TaskBoardLiteDbContext dbContext,
        ILogger<DatabaseInitializer> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task ApplyMigrationsAndSeedDevelopmentDataAsync(CancellationToken cancellationToken)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);

        if (await _dbContext.Projects.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        var project = new Project(
            "Development Sample Project",
            "DEV",
            "Development seed data for local API exploration.",
            now);

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var firstItem = new WorkItem(
            project.Id,
            "Review task board API",
            "Seeded item used only in Development when the database is empty.",
            WorkItemPriority.High,
            now.AddDays(5),
            now);

        var secondItem = new WorkItem(
            project.Id,
            "Write interview notes",
            "Seeded item used only in Development when the database is empty.",
            WorkItemPriority.Medium,
            null,
            now);

        _dbContext.WorkItems.AddRange(firstItem, secondItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded development data for TaskBoard Lite API.");
    }
}
