using TaskBoardLite.Domain.Enums;
using TaskBoardLite.Domain.Exceptions;

namespace TaskBoardLite.Domain.Entities;

public sealed class WorkItem
{
    private static readonly IReadOnlyDictionary<WorkItemStatus, WorkItemStatus[]> AllowedTransitions =
        new Dictionary<WorkItemStatus, WorkItemStatus[]>
        {
            [WorkItemStatus.Todo] = [WorkItemStatus.InProgress, WorkItemStatus.Cancelled],
            [WorkItemStatus.InProgress] = [WorkItemStatus.Todo, WorkItemStatus.Done, WorkItemStatus.Cancelled],
            [WorkItemStatus.Done] = [WorkItemStatus.InProgress],
            [WorkItemStatus.Cancelled] = [WorkItemStatus.Todo]
        };

    private readonly List<WorkItemComment> _comments = [];

    private WorkItem()
    {
        Title = string.Empty;
    }

    public WorkItem(
        int projectId,
        string title,
        string? description,
        WorkItemPriority priority,
        DateTimeOffset? dueDateUtc,
        DateTimeOffset createdAtUtc)
    {
        if (projectId <= 0)
        {
            throw new DomainValidationException("ProjectId must refer to an existing project.");
        }

        ProjectId = projectId;
        Title = ValidateRequiredLength(title, nameof(Title), 3, 150);
        Description = ValidateOptionalLength(description, nameof(Description), 2000);
        Priority = priority;
        DueDateUtc = dueDateUtc;
        Status = WorkItemStatus.Todo;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        Version = 1;
    }

    public int Id { get; private set; }

    public int ProjectId { get; private set; }

    public Project? Project { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public WorkItemStatus Status { get; private set; }

    public WorkItemPriority Priority { get; private set; }

    public DateTimeOffset? DueDateUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<WorkItemComment> Comments => _comments.AsReadOnly();

    public void UpdateDetails(
        string title,
        string? description,
        WorkItemPriority priority,
        DateTimeOffset? dueDateUtc,
        DateTimeOffset updatedAtUtc)
    {
        Title = ValidateRequiredLength(title, nameof(Title), 3, 150);
        Description = ValidateOptionalLength(description, nameof(Description), 2000);
        Priority = priority;
        DueDateUtc = dueDateUtc;
        Touch(updatedAtUtc);
    }

    public void ChangeStatus(WorkItemStatus newStatus, DateTimeOffset updatedAtUtc)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (!AllowedTransitions[Status].Contains(newStatus))
        {
            throw new InvalidStatusTransitionException(Status, newStatus);
        }

        Status = newStatus;
        Touch(updatedAtUtc);
    }

    private void Touch(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
        Version++;
    }

    private static string ValidateRequiredLength(string? value, string fieldName, int minLength, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length < minLength || trimmed.Length > maxLength)
        {
            throw new DomainValidationException($"{fieldName} must be between {minLength} and {maxLength} characters.");
        }

        return trimmed;
    }

    private static string? ValidateOptionalLength(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainValidationException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return trimmed;
    }
}
