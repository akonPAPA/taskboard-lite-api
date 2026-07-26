using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskBoardLite.Domain.Exceptions;

namespace TaskBoardLite.Api.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ApiExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<ApiExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            DomainValidationException => (StatusCodes.Status400BadRequest, "Validation failed.", exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource was not found.", exception.Message),
            DuplicateValueException => (StatusCodes.Status409Conflict, "Duplicate value conflict.", exception.Message),
            InvalidStatusTransitionException => (StatusCodes.Status409Conflict, "Invalid status transition.", exception.Message),
            OptimisticConcurrencyException => (StatusCodes.Status409Conflict, "Optimistic concurrency conflict.", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Request conflicts with current state.", exception.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Optimistic concurrency conflict.", "The resource was changed by another request."),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error.", "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

            if (_environment.IsEnvironment("Testing"))
            {
                detail = $"{exception.GetType().Name}: {exception.Message}";
            }
        }
        else
        {
            _logger.LogInformation("Handled API exception {ExceptionType} as {StatusCode} for {Method} {Path}.",
                exception.GetType().Name,
                statusCode,
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = $"https://httpstatuses.com/{statusCode}",
                Instance = httpContext.Request.Path
            },
            Exception = exception
        });
    }
}
