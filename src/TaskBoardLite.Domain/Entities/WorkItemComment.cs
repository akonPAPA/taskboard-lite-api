using TaskBoardLite.Domain.Exceptions;

namespace TaskBoardLite.Domain.Entities;

public sealed class WorkItemComment
{
    private WorkItemComment()
    {
        AuthorName = string.Empty;
        Body = string.Empty;
    }

    public WorkItemComment(int workItemId, string authorName, string body, DateTimeOffset createdAtUtc)
    {
        if (workItemId <= 0)
        {
            throw new DomainValidationException("WorkItemId must refer to an existing work item.");
        }

        WorkItemId = workItemId;
        AuthorName = ValidateRequiredLength(authorName, nameof(AuthorName), 1, 100);
        Body = ValidateRequiredLength(body, nameof(Body), 1, 1000);
        CreatedAtUtc = createdAtUtc;
    }

    public int Id { get; private set; }

    public int WorkItemId { get; private set; }

    public WorkItem? WorkItem { get; private set; }

    public string AuthorName { get; private set; }

    public string Body { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

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
}
