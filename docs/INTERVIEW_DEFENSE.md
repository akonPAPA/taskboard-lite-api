# Interview Defense

## 1. Why did you select this project?

It is small enough to explain clearly but includes real backend concerns: validation, relational data, filtering, status rules, comments, tests, and concurrency.

## 2. Why did you separate Domain, Infrastructure, and API?

The separation keeps business rules in `TaskBoardLite.Domain`, database details in `TaskBoardLite.Infrastructure`, and HTTP behavior in `TaskBoardLite.Api`. This makes dependencies easier to explain and test.

## 3. Why are EF entities not returned directly?

The API returns DTOs from `TaskBoardLite.Api.Contracts`. This prevents database-shaped objects and navigation properties from becoming the public contract.

## 4. How does dependency injection work in this project?

`Program.cs` registers controllers, services, `TimeProvider`, exception handling, and infrastructure. Controllers receive services through constructors. Services receive `TaskBoardLiteDbContext` and `TimeProvider` through constructors.

## 5. Why is DbContext scoped?

A scoped `DbContext` matches one HTTP request. It tracks changes for that request and is disposed after the request finishes.

## 6. How does a request travel through the application?

The request reaches a controller, is bound to a DTO, is validated, then the controller calls a service. The service uses domain entities and EF Core, then maps the result back to a response DTO.

## 7. Where are business rules enforced?

Status transitions and entity state rules are enforced in `WorkItem`, `Project`, and `WorkItemComment` in the domain project. Services enforce workflow rules such as project existence and stale version checks.

## 8. How does filtering translate into SQL?

`WorkItemService.ListAsync` composes an `IQueryable`. EF Core translates the applied `Where`, `OrderBy`, `Skip`, and `Take` calls into SQL for SQLite.

## 9. Why is AsNoTracking used for reads?

Read endpoints do not update the loaded entities. `AsNoTracking` avoids unnecessary change tracking and makes the intent clear.

## 10. How does optimistic concurrency work?

A work item response includes `version`. The client sends that version with update or status-change requests. If the stored version has changed, the API returns `409 Conflict`.

## 11. What race condition does the version field prevent?

It prevents a stale update from overwriting another update made after the stale client originally read the work item.

## 12. Why is project deletion restricted?

Deleting a project with work items could silently remove useful work history. The API returns `409 Conflict` instead, so the client must delete or move work items intentionally.

## 13. How do HTTP status codes map to failures?

Validation failures return `400`, missing resources return `404`, duplicate codes and business conflicts return `409`, and unexpected failures return `500` without stack traces in normal responses.

## 14. What is tested with unit tests?

Unit tests cover status transitions, invalid transitions, project code normalization, entity validation, work-item defaults, version increments, pagination validation, sorting validation, and sorting behavior.

## 15. What is tested with integration tests?

Integration tests cover project creation, duplicate code rejection, work-item creation, missing project rejection, filtering, valid and invalid status changes, stale concurrency updates, project deletion conflict, and validation ProblemDetails.

## 16. What would need to change before production use?

The project would need authentication, authorization, real users, secret management, monitoring, rate limiting, deployment configuration, and environment-specific database planning.

## 17. What would you improve with more time?

Add user accounts, assigned users, project update endpoints, richer audit history, and more tests around edge cases and database migrations.

## 18. What parts are basic Junior-level decisions?

Controllers, DTOs, EF Core with SQLite, xUnit tests, data annotations, explicit mapping, and simple services are all understandable Junior-level choices.

## 19. Which implementation decisions have trade-offs?

SQLite is easy to run but lacks some database features. Integer `Version` concurrency is simple but not distributed locking. A hand-authored OpenAPI JSON endpoint avoids a vulnerable package chain but does not provide Swagger UI.

## 20. How is the project different from a trivial CRUD tutorial?

It includes domain status rules, filtered and paginated queries, comments, delete restrictions, centralized error handling, migrations, concurrency checks, and integration tests against SQLite.
