using System.ComponentModel.DataAnnotations;
using TaskBoardLite.Api.Contracts;
using TaskBoardLite.Api.Services;
using TaskBoardLite.Domain.Entities;
using TaskBoardLite.Domain.Enums;
using TaskBoardLite.Domain.Exceptions;

namespace TaskBoardLite.UnitTests;

public sealed class DomainRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.Cancelled)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Todo)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Done)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Cancelled)]
    [InlineData(WorkItemStatus.Done, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.Cancelled, WorkItemStatus.Todo)]
    public void ChangeStatus_AllowsDocumentedTransitions(WorkItemStatus from, WorkItemStatus to)
    {
        var workItem = WorkItemInStatus(from);

        workItem.ChangeStatus(to, Now.AddMinutes(1));

        Assert.Equal(to, workItem.Status);
    }

    [Theory]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.Done)]
    [InlineData(WorkItemStatus.Done, WorkItemStatus.Todo)]
    [InlineData(WorkItemStatus.Cancelled, WorkItemStatus.Done)]
    public void ChangeStatus_RejectsForbiddenTransitions(WorkItemStatus from, WorkItemStatus to)
    {
        var workItem = WorkItemInStatus(from);

        Assert.Throws<InvalidStatusTransitionException>(() => workItem.ChangeStatus(to, Now.AddMinutes(1)));
    }

    [Fact]
    public void Project_NormalizesCodeToUppercase()
    {
        var project = new Project("Api Work", "tb", null, Now);

        Assert.Equal("TB", project.Code);
    }

    [Fact]
    public void Project_RejectsShortName()
    {
        Assert.Throws<DomainValidationException>(() => new Project("AB", "ABC", null, Now));
    }

    [Fact]
    public void WorkItem_CreationSetsDefaults()
    {
        var workItem = new WorkItem(1, "Build API", null, WorkItemPriority.High, null, Now);

        Assert.Equal(WorkItemStatus.Todo, workItem.Status);
        Assert.Equal(1, workItem.Version);
        Assert.Equal(Now, workItem.CreatedAtUtc);
        Assert.Equal(Now, workItem.UpdatedAtUtc);
    }

    [Fact]
    public void WorkItem_UpdateDetailsIncrementsVersionAndTimestamp()
    {
        var workItem = new WorkItem(1, "Build API", null, WorkItemPriority.High, null, Now);
        var updatedAt = Now.AddHours(2);

        workItem.UpdateDetails("Build tested API", "Updated", WorkItemPriority.Critical, updatedAt.AddDays(1), updatedAt);

        Assert.Equal(2, workItem.Version);
        Assert.Equal(updatedAt, workItem.UpdatedAtUtc);
        Assert.Equal("Build tested API", workItem.Title);
    }

    [Fact]
    public void WorkItem_RejectsBlankTitle()
    {
        Assert.Throws<DomainValidationException>(() => new WorkItem(1, " ", null, WorkItemPriority.Medium, null, Now));
    }

    [Fact]
    public void WorkItemComment_RejectsBlankBody()
    {
        Assert.Throws<DomainValidationException>(() => new WorkItemComment(1, "Mira", " ", Now));
    }

    [Fact]
    public void WorkItemQueryParameters_DefaultsAreValid()
    {
        var query = new WorkItemQueryParameters();

        var errors = Validate(query);

        Assert.Empty(errors);
        Assert.Equal(1, query.Page);
        Assert.Equal(20, query.PageSize);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void WorkItemQueryParameters_RejectsInvalidPagination(int page, int pageSize)
    {
        var query = new WorkItemQueryParameters { Page = page, PageSize = pageSize };

        var errors = Validate(query);

        Assert.NotEmpty(errors);
    }

    [Theory]
    [InlineData("priority", "desc")]
    [InlineData("createdAt", "down")]
    public void WorkItemQueryParameters_RejectsInvalidSorting(string sortBy, string sortDirection)
    {
        var query = new WorkItemQueryParameters { SortBy = sortBy, SortDirection = sortDirection };

        var errors = Validate(query);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ApplySorting_SortsByCreatedAtAscending()
    {
        var first = new WorkItem(1, "First item", null, WorkItemPriority.Medium, null, Now);
        var second = new WorkItem(1, "Second item", null, WorkItemPriority.Medium, null, Now.AddHours(1));

        var sorted = WorkItemService.ApplySorting(new[] { second, first }.AsQueryable(), "createdAt", "asc").ToList();

        Assert.Same(first, sorted[0]);
        Assert.Same(second, sorted[1]);
    }

    private static WorkItem WorkItemInStatus(WorkItemStatus status)
    {
        var workItem = new WorkItem(1, "Build API", null, WorkItemPriority.Medium, null, Now);
        return status switch
        {
            WorkItemStatus.Todo => workItem,
            WorkItemStatus.InProgress => Change(workItem, WorkItemStatus.InProgress),
            WorkItemStatus.Done => Change(Change(workItem, WorkItemStatus.InProgress), WorkItemStatus.Done),
            WorkItemStatus.Cancelled => Change(workItem, WorkItemStatus.Cancelled),
            _ => workItem
        };
    }

    private static WorkItem Change(WorkItem workItem, WorkItemStatus status)
    {
        workItem.ChangeStatus(status, Now.AddMinutes(1));
        return workItem;
    }

    private static IReadOnlyList<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
        return results;
    }
}
