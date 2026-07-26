using System.ComponentModel.DataAnnotations;

namespace TaskBoardLite.Api.Contracts;

public sealed record CreateProjectRequest(
    [Required]
    [StringLength(100, MinimumLength = 3)]
    string? Name,

    [Required]
    [StringLength(20, MinimumLength = 2)]
    string? Code,

    [StringLength(500)]
    string? Description);

public sealed record ProjectResponse(
    int Id,
    string Name,
    string Code,
    string? Description,
    DateTimeOffset CreatedAtUtc);

