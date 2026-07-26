using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TaskBoardLite.Api.Contracts;
using TaskBoardLite.Domain.Enums;

namespace TaskBoardLite.IntegrationTests;

public sealed class ApiScenarioTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task CreateProject_ReturnsCreatedProject()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();

        var response = await PostJsonAsync(client, "/api/projects", new
        {
            name = "Task Board",
            code = "tb",
            description = "Internship demo project"
        });

        await AssertStatusAsync(response, HttpStatusCode.Created);
        Assert.NotNull(response.Headers.Location);
        var project = await ReadAsync<ProjectResponse>(response);
        Assert.Equal("TB", project.Code);
    }

    [Fact]
    public async Task CreateProject_RejectsDuplicateCode()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();
        await CreateProjectAsync(client, "First Project", "DUP");

        var response = await PostJsonAsync(client, "/api/projects", new { name = "Second Project", code = "dup" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkItem_UnderExistingProject_ReturnsCreatedItem()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Task Board", "TB");

        var workItem = await CreateWorkItemAsync(client, project.Id, "Build endpoint", "High");

        Assert.Equal(project.Id, workItem.ProjectId);
        Assert.Equal(WorkItemStatus.Todo, workItem.Status);
        Assert.Equal(1, workItem.Version);
    }

    [Fact]
    public async Task CreateWorkItem_UnderMissingProject_ReturnsNotFound()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();

        var response = await PostJsonAsync(client, "/api/projects/999/work-items", new
        {
            title = "Build endpoint",
            priority = "Medium"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListWorkItems_FiltersByStatus()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Task Board", "TB");
        var first = await CreateWorkItemAsync(client, project.Id, "Build endpoint", "High");
        await CreateWorkItemAsync(client, project.Id, "Write docs", "Medium");
        await PatchJsonAsync(client, $"/api/work-items/{first.Id}/status", new { status = "InProgress", version = first.Version });

        var response = await client.GetAsync($"/api/projects/{project.Id}/work-items?status=InProgress");

        await AssertStatusAsync(response, HttpStatusCode.OK);
        var page = await ReadAsync<PagedResponse<WorkItemResponse>>(response);
        Assert.Single(page.Items);
        Assert.Equal("Build endpoint", page.Items[0].Title);
    }

    [Fact]
    public async Task ChangeStatus_AllowsValidTransition()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Task Board", "TB");
        var workItem = await CreateWorkItemAsync(client, project.Id, "Build endpoint", "High");

        var response = await PatchJsonAsync(client, $"/api/work-items/{workItem.Id}/status", new
        {
            status = "InProgress",
            version = workItem.Version
        });

        await AssertStatusAsync(response, HttpStatusCode.OK);
        var updated = await ReadAsync<WorkItemResponse>(response);
        Assert.Equal(WorkItemStatus.InProgress, updated.Status);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public async Task ChangeStatus_RejectsInvalidTransition()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Task Board", "TB");
        var workItem = await CreateWorkItemAsync(client, project.Id, "Build endpoint", "High");

        var response = await PatchJsonAsync(client, $"/api/work-items/{workItem.Id}/status", new
        {
            status = "Done",
            version = workItem.Version
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkItem_RejectsStaleVersion()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Task Board", "TB");
        var workItem = await CreateWorkItemAsync(client, project.Id, "Build endpoint", "High");

        var firstUpdate = await PutJsonAsync(client, $"/api/work-items/{workItem.Id}", new
        {
            title = "Build tested endpoint",
            description = "Updated once",
            priority = "Critical",
            dueDateUtc = (DateTimeOffset?)null,
            version = workItem.Version
        });
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        var staleUpdate = await PutJsonAsync(client, $"/api/work-items/{workItem.Id}", new
        {
            title = "Build stale endpoint",
            description = "This uses the stale original version",
            priority = "Medium",
            dueDateUtc = (DateTimeOffset?)null,
            version = workItem.Version
        });

        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_WithWorkItems_ReturnsConflict()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Task Board", "TB");
        await CreateWorkItemAsync(client, project.Id, "Build endpoint", "High");

        var response = await client.DeleteAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ValidationFailure_ReturnsProblemDetails()
    {
        using var factory = new TaskBoardLiteApiFactory();
        using var client = factory.CreateClient();

        var response = await PostJsonAsync(client, "/api/projects", new { code = "TB" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadAsync<ProblemDetails>(response);
        Assert.Equal(400, problem.Status);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, string name, string code)
    {
        var response = await PostJsonAsync(client, "/api/projects", new { name, code });
        await AssertStatusAsync(response, HttpStatusCode.Created);
        return await ReadAsync<ProjectResponse>(response);
    }

    private static async Task<WorkItemResponse> CreateWorkItemAsync(HttpClient client, int projectId, string title, string priority)
    {
        var response = await PostJsonAsync(client, $"/api/projects/{projectId}/work-items", new { title, priority });
        await AssertStatusAsync(response, HttpStatusCode.Created);
        return await ReadAsync<WorkItemResponse>(response);
    }

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object body) =>
        client.PostAsJsonAsync(url, body, JsonOptions);

    private static Task<HttpResponseMessage> PutJsonAsync(HttpClient client, string url, object body) =>
        client.PutAsJsonAsync(url, body, JsonOptions);

    private static Task<HttpResponseMessage> PatchJsonAsync(HttpClient client, string url, object body) =>
        client.PatchAsJsonAsync(url, body, JsonOptions);

    private static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected {expected}, got {response.StatusCode}. Body: {body}");
        }
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        Assert.NotNull(value);
        return value;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}




