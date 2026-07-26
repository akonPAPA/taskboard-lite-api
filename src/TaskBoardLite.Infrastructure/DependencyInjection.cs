using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskBoardLite.Infrastructure.Persistence;

namespace TaskBoardLite.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TaskBoardLite")
            ?? "Data Source=taskboardlite.db";

        services.AddDbContext<TaskBoardLiteDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
