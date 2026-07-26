using TaskBoardLite.Api.Contracts;
using TaskBoardLite.Domain.Entities;

namespace TaskBoardLite.Api.Mapping;

public static class DtoMappings
{
    public static ProjectResponse ToResponse(this Project project) =>
        new(project.Id, project.Name, project.Code, project.Description, project.CreatedAtUtc);

    public static WorkItemResponse ToResponse(this WorkItem workItem) =>
        new(
            workItem.Id,
            workItem.ProjectId,
            workItem.Title,
            workItem.Description,
            workItem.Status,
            workItem.Priority,
            workItem.DueDateUtc,
            workItem.CreatedAtUtc,
            workItem.UpdatedAtUtc,
            workItem.Version);

    public static CommentResponse ToResponse(this WorkItemComment comment) =>
        new(comment.Id, comment.WorkItemId, comment.AuthorName, comment.Body, comment.CreatedAtUtc);
}
