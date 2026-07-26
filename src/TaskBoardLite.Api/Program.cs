using System.Text.Json.Serialization;
using TaskBoardLite.Api.Errors;
using TaskBoardLite.Api.OpenApi;
using TaskBoardLite.Api.Services;
using TaskBoardLite.Infrastructure;
using TaskBoardLite.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<WorkItemService>();
builder.Services.AddScoped<CommentService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    OpenApiDocumentFactory.MapOpenApiEndpoint(app);

    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.ApplyMigrationsAndSeedDevelopmentDataAsync(CancellationToken.None);
}

app.MapControllers();

await app.RunAsync();

public partial class Program
{
}
