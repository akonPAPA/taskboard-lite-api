using System.ComponentModel.DataAnnotations;
using TaskBoardLite.Domain.Enums;

namespace TaskBoardLite.Api.Contracts;

public sealed class WorkItemQueryParameters : IValidatableObject
{
    private static readonly string[] AllowedSortFields = ["createdAt", "dueDate"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public WorkItemStatus? Status { get; init; }

    public WorkItemPriority? Priority { get; init; }

    public DateTimeOffset? DueBeforeUtc { get; init; }

    public string? Search { get; init; }

    public string SortBy { get; init; } = "createdAt";

    public string SortDirection { get; init; } = "desc";

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Page < 1)
        {
            yield return new ValidationResult("Page must be at least 1.", [nameof(Page)]);
        }

        if (PageSize < 1 || PageSize > 100)
        {
            yield return new ValidationResult("PageSize must be between 1 and 100.", [nameof(PageSize)]);
        }

        if (!AllowedSortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("SortBy must be either createdAt or dueDate.", [nameof(SortBy)]);
        }

        if (!AllowedSortDirections.Contains(SortDirection, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("SortDirection must be either asc or desc.", [nameof(SortDirection)]);
        }
    }
}

