using System.ComponentModel.DataAnnotations;

namespace TaskBoardLite.Api.Contracts;

public sealed record CreateCommentRequest(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string? AuthorName,

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    string? Body);

public sealed record CommentResponse(
    int Id,
    int WorkItemId,
    string AuthorName,
    string Body,
    DateTimeOffset CreatedAtUtc);

