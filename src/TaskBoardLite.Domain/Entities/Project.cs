using TaskBoardLite.Domain.Exceptions;

namespace TaskBoardLite.Domain.Entities;

public sealed class Project
{
    private readonly List<WorkItem> _workItems = [];

    private Project()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public Project(string name, string code, string? description, DateTimeOffset createdAtUtc)
    {
        Name = ValidateRequiredLength(name, nameof(Name), 3, 100);
        Code = ValidateRequiredLength(code, nameof(Code), 2, 20).ToUpperInvariant();
        Description = ValidateOptionalLength(description, nameof(Description), 500);
        CreatedAtUtc = createdAtUtc;
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<WorkItem> WorkItems => _workItems.AsReadOnly();

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
