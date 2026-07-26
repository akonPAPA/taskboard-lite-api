using System.ComponentModel.DataAnnotations;
using TaskBoardLite.Domain.Enums;

namespace TaskBoardLite.Api.Contracts;

public sealed record CreateWorkItemRequest(
    [Required]
    [StringLength(150, MinimumLength = 3)]
    string? Title,

    [StringLength(2000)]
    string? Description,

    WorkItemPriority Priority,

    DateTimeOffset? DueDateUtc);

public sealed record UpdateWorkItemRequest(
    [Required]
    [StringLength(150, MinimumLength = 3)]
    string? Title,

    [StringLength(2000)]
    string? Description,

    WorkItemPriority Priority,

    DateTimeOffset? DueDateUtc,

    [Range(1, int.MaxValue)]
    int Version);

public sealed record ChangeWorkItemStatusRequest(
    WorkItemStatus Status,

    [Range(1, int.MaxValue)]
    int Version);

public sealed record WorkItemResponse(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    WorkItemStatus Status,
    WorkItemPriority Priority,
    DateTimeOffset? DueDateUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

