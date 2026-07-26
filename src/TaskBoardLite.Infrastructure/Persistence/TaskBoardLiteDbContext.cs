using Microsoft.EntityFrameworkCore;
using TaskBoardLite.Domain.Entities;

namespace TaskBoardLite.Infrastructure.Persistence;

public sealed class TaskBoardLiteDbContext : DbContext
{
    public TaskBoardLiteDbContext(DbContextOptions<TaskBoardLiteDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    public DbSet<WorkItemComment> WorkItemComments => Set<WorkItemComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskBoardLiteDbContext).Assembly);
    }
}
