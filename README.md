# TaskBoard Lite API

This project is a learning-focused backend app created to show off fundamental .NET dev skills. TaskBoard Lite API is a small REST API for managing team projects, wrok items, comments, priorities, statuses, due dates, filtering, and optimistic concurrency.


<a href="https://www.youtube.com/shorts/j2MpboPLs0g?feature=share">
    <img src="" alt="click to see the problem">
</a>


## Implemented Functionality

- Create, list, read, and delete projects.
- Reject duplicate project codes.
- Reject deleting a project that still has wrok items.
- Create, list, read, update, change status, and delete work items.
- Filter work items by status, priority, due date, and title search.
- Sort work items by creation date or due date, ascending or descending.
- Paginate work item results with validation.
- Add and list work item comments.
- Return ProblemDetails for validation, not-found, conflict, and unexpected errors.
- Use a Version field for optimistic concurrency on work item updates and status changes.
- Apply EF Core migrations automatically only in Development.
- Seed small dev-only data only when the db is empty.

## Tech Stack

- .NET 10.0
- C# with nullable reference types
- ASP.NET Core Web API controllers
- Entity Framework Core
- SQLite
- xUnit
- WebApplicationFactory integration tests
- GitHub Actions CI

## Architecture Overview

The solution uses three runtime projects:

- `TaskBoardLite.Domain`: entities, enums, validation rules, and status-transition rules. It has no ASP.NET Core or EF Core dependency.
- `TaskBoardLite.Infrastructure`: EF Core `DbContext`, Fluent API config, migrations, SQLite setup, and dev db initialization.
- `TaskBoardLite.Api`: controllers, DTOs, explicit mapping, app services, DI, HTTP behavior, and centralized exception handling.

Btw, controllers just accept HTTP requests and delegate to services. Services orchestrate EF Core and domain objects. Domain entities enforce rules like valid status transitions. EF Core stores the data in SQLite.

### Domain Rules

Project rules:

- `Name` is required and must be 3 to 100 chars.
- `Code` is required, must be 2 to 20 chars, is normalized to uppercase, and is unique.
- `Description` is optional and limited to 500 chars.
- `CreatedAtUtc` is assigned by the app.

Work item rules:

- `Title` is required and must be 3 to 150 chars.
- `Description` is optional and limited to 2,000 chars.
- New work items start in `Todo`.
- Status changes must use `ChangeStatus` and follow the allowed transition table.
- `Version` is checked and incremented for updates and status changes.

Allowed status transitions:

```text
Todo       -> InProgress, Cancelled
InProgress -> Todo, Done, Cancelled
Done       -> InProgress
Cancelled  -> Todo
```

Comment rules:

- `AuthorName` is required and limited to 100 chars.
- `Body` is required and limited to 1,000 chars.

## Database Schema Overview

Tables:

- `Projects`: project identity, name, code, description, creation timestamp.
- `WorkItems`: project relationship, title, description, status, priority, due date, timestamps, version.
- `WorkItemComments`: work item relationship, author, body, creation timestamp.

Indexes and constraints:

- Unique index on `Projects.Code`.
- Foreign key from `WorkItems.ProjectId` to `Projects.Id` with restricted delete behavior.
- Foreign key from `WorkItemComments.WorkItemId` to `WorkItems.Id`.
- Indexes on `WorkItems.ProjectId`, `Status`, `Priority`, and a composite filtering index.

`DateTimeOffset` values are stored as UTC ticks so SQLite can sort and filter them reliably. Btw, this avoids a lot of timezone headaches.

## Run On Windows PowerShell

```powershell
cd C:\Users\mukha\Documents\Codex\2026-07-26\files-mentioned-by-the-user-you\outputs\TaskBoardLite

dotnet tool restore
dotnet restore
dotnet run --project .\src\TaskBoardLite.Api\TaskBoardLite.Api.csproj
```

Expected result:

```text
Now listening on: http://localhost:5xxx
Application started. Press Ctrl+C to shut down.
```

In `Development`, the API exposes OpenAPI JSON at:

```text
http://localhost:5xxx/openapi/v1.json
```

## Create Or Update The Database

Dev startup applies migrations automatically. If u wanna apply migrations manually:

```powershell
cd C:\Users\mukha\Documents\Codex\2026-07-26\files-mentioned-by-the-user-you\outputs\TaskBoardLite

dotnet tool restore
dotnet tool run dotnet-ef database update --project .\src\TaskBoardLite.Infrastructure\TaskBoardLite.Infrastructure.csproj --startup-project .\src\TaskBoardLite.Api\TaskBoardLite.Api.csproj
```

Expected result:

```text
Done.
```

## Run Tests

```powershell
cd C:\Users\mukha\Documents\Codex\2026-07-26\files-mentioned-by-the-user-you\outputs\TaskBoardLite

dotnet test --configuration Release
```

Expected result:

```text
Passed! - TaskBoardLite.UnitTests.dll
Passed! - TaskBoardLite.IntegrationTests.dll
```

## Example API Requests

Set a base URL after starting the API:

```powershell
$baseUrl = "http://localhost:5000"
```

Create a project:

```powershell
curl -i -X POST "$baseUrl/api/projects" -H "Content-Type: application/json" -d "{\"name\":\"Task Board\",\"code\":\"tb\",\"description\":\"Demo project\"}"
```

```powershell
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/projects" -ContentType "application/json" -Body '{"name":"Task Board","code":"tb","description":"Demo project"}'
```

Expected response: `201 Created` with an uppercase `code` and a `Location` header.

Create a work item:

```powershell
curl -i -X POST "$baseUrl/api/projects/1/work-items" -H "Content-Type: application/json" -d "{\"title\":\"Build endpoint\",\"priority\":\"High\",\"dueDateUtc\":\"2026-08-15T00:00:00Z\"}"
```

```powershell
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/projects/1/work-items" -ContentType "application/json" -Body '{"title":"Build endpoint","priority":"High","dueDateUtc":"2026-08-15T00:00:00Z"}'
```

Expected response: `201 Created` with `status` set to `Todo` and `version` set to `1`.

List filtered work items:

```powershell
curl -i "$baseUrl/api/projects/1/work-items?status=InProgress&priority=High&page=1&pageSize=20&sortBy=dueDate&sortDirection=asc"
```

```powershell
Invoke-RestMethod -Method Get -Uri "$baseUrl/api/projects/1/work-items?status=InProgress&priority=High&page=1&pageSize=20&sortBy=dueDate&sortDirection=asc"
```

Expected response: `200 OK` with `items`, `page`, `pageSize`, `totalRecords`, and `totalPages`.

Change status:

```powershell
curl -i -X PATCH "$baseUrl/api/work-items/1/status" -H "Content-Type: application/json" -d "{\"status\":\"InProgress\",\"version\":1}"
```

```powershell
Invoke-RestMethod -Method Patch -Uri "$baseUrl/api/work-items/1/status" -ContentType "application/json" -Body '{"status":"InProgress","version":1}'
```

Expected response: `200 OK` with status `InProgress` and incremented `version`.

Provoke an invalid transition:

```powershell
curl -i -X PATCH "$baseUrl/api/work-items/1/status" -H "Content-Type: application/json" -d "{\"status\":\"Done\",\"version\":1}"
```

```powershell
Invoke-RestMethod -Method Patch -Uri "$baseUrl/api/work-items/1/status" -ContentType "application/json" -Body '{"status":"Done","version":1}'
```

Expected response: `409 Conflict` with `ProblemDetails`.

Provoke a concurrency conflict:

```powershell
curl -i -X PUT "$baseUrl/api/work-items/1" -H "Content-Type: application/json" -d "{\"title\":\"First update\",\"description\":null,\"priority\":\"High\",\"dueDateUtc\":null,\"version\":1}"
curl -i -X PUT "$baseUrl/api/work-items/1" -H "Content-Type: application/json" -d "{\"title\":\"Stale update\",\"description\":null,\"priority\":\"Medium\",\"dueDateUtc\":null,\"version\":1}"
```

```powershell
Invoke-RestMethod -Method Put -Uri "$baseUrl/api/work-items/1" -ContentType "application/json" -Body '{"title":"First update","description":null,"priority":"High","dueDateUtc":null,"version":1}'
Invoke-RestMethod -Method Put -Uri "$baseUrl/api/work-items/1" -ContentType "application/json" -Body '{"title":"Stale update","description":null,"priority":"Medium","dueDateUtc":null,"version":1}'
```

Expected response: first request `200 OK`, second request `409 Conflict`.

Add a comment:

```powershell
curl -i -X POST "$baseUrl/api/work-items/1/comments" -H "Content-Type: application/json" -d "{\"authorName\":\"Mira\",\"body\":\"Looks good.\"}"
```
```powershell
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/work-items/1/comments" -ContentType "application/json" -Body '{"authorName":"Mira","body":"Looks good."}'
```

Expected response: `201 Created` with the saved comment.

Delete a work item:

```powershell
curl -i -X DELETE "$baseUrl/api/work-items/1"
```

```powershell
Invoke-RestMethod -Method Delete -Uri "$baseUrl/api/work-items/1"
```

Expected response: `204 No Content`.

## Design Decisions

- Controllers are used cuz theyre easy to explain in an interview.
- DTOs are used so EF Core entities aren't exposed directly.
- EF Core is used directly in services instead of a generic repo.
- Status transition rules live in the domain entity.
- SQLite is used so the app runs locally without Docker or a seperate db server.
- Optimistic concurrency is implemented with an integer `Version` cuz SQLite doesn't have SQL Server-style rowversion.

## Known Limitations

- No real auth or authorization.
- No user accounts.
- No prod secret storage.
- No rate limiting.
- No monitoring or tracing setup beyond normal logging.
- No distributed cache.
- No deployment infra.
- OpenAPI is exposed as JSON; a Swagger UI package isn't included cuz the available ASP.NET Core OpenAPI dependency chain raised NuGet audit warnings in this enviroment.

## Future Improvements

- Add auth and per-user authorization.
- Add richer project update endpoints.
- Add assigned users after user accounts exist.
- Add more filtering options after real usage patterns are known.
- Add deployment config when there's a real target env.

## Exact Verification Status

Verified locally on .NET SDK `10.0.301`:

- `dotnet restore`: passed.
- `dotnet build --configuration Release`: passed with zero warnings.
- `dotnet test --configuration Release`: passed with 23 unit tests and 10 integration tests.
- API startup: verified locally.
- Manual HTTP checks: project creation, duplicate project rejection, wrok-item creation, filtering, valid status transition, invalid status transition, optimistic concurrency conflict, comment creation, and project deletion conflict were verified locally.

