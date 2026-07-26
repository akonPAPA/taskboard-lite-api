namespace TaskBoardLite.Api.OpenApi;

public static class OpenApiDocumentFactory
{
    public static void MapOpenApiEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/openapi/v1.json", () => Results.Json(CreateDocument()));
    }

    private static object CreateDocument() => new
    {
        openapi = "3.1.0",
        info = new
        {
            title = "TaskBoard Lite API",
            version = "v1",
            description = "Learning-focused task management REST API."
        },
        paths = new Dictionary<string, object>
        {
            ["/api/projects"] = new
            {
                post = Operation("Create a project", "201"),
                get = Operation("List projects", "200")
            },
            ["/api/projects/{id}"] = new
            {
                get = Operation("Get a project", "200"),
                delete = Operation("Delete an empty project", "204")
            },
            ["/api/projects/{projectId}/work-items"] = new
            {
                post = Operation("Create a work item in a project", "201"),
                get = Operation("List and filter work items in a project", "200")
            },
            ["/api/work-items/{id}"] = new
            {
                get = Operation("Get a work item", "200"),
                put = Operation("Update a work item with optimistic concurrency", "200"),
                delete = Operation("Delete a work item", "204")
            },
            ["/api/work-items/{id}/status"] = new
            {
                patch = Operation("Change work item status with transition validation", "200")
            },
            ["/api/work-items/{workItemId}/comments"] = new
            {
                post = Operation("Add a comment to a work item", "201"),
                get = Operation("List comments for a work item", "200")
            }
        }
    };

    private static object Operation(string summary, string successStatus) => new
    {
        summary,
        responses = new Dictionary<string, object>
        {
            [successStatus] = new { description = "Success" },
            ["400"] = new { description = "Validation failure" },
            ["404"] = new { description = "Resource not found" },
            ["409"] = new { description = "Conflict" },
            ["500"] = new { description = "Unexpected server error" }
        }
    };
}
