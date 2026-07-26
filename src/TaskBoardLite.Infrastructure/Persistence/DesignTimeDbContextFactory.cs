using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskBoardLite.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaskBoardLiteDbContext>
{
    public TaskBoardLiteDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TaskBoardLiteDbContext>()
            .UseSqlite("Data Source=taskboardlite.db")
            .Options;

        return new TaskBoardLiteDbContext(options);
    }
}
