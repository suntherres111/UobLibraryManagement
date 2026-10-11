using LibraryManagement.Application.Common;
using LibraryManagement.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Infrastructure;

/// <summary>
/// Translates exceptions into RFC 7807 ProblemDetails responses so that controllers stay free
/// of try/catch blocks and every error has the same shape. Unexpected errors never leak
/// internal details to the client.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", "NOT_FOUND"),
            DomainValidationException e => (StatusCodes.Status400BadRequest, "Validation failed", e.Code),
            DomainException e => (StatusCodes.Status409Conflict, "Business rule violation", e.Code),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "INTERNAL_ERROR")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogInformation("Request rejected with {Code}: {Message}", code, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);

        return true;
    }
}
