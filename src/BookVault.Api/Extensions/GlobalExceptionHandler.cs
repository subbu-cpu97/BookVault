using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookVault.Api.Extensions;

// IExceptionHandler = .NET 8+ way to handle exceptions globally
// Interview answer: "Why a global exception handler?"
// Without it, every endpoint needs try/catch — repeated code everywhere.
// One handler catches all exceptions and maps them to RFC 7807 ProblemDetails
// responses — the industry standard JSON error format.
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            ValidationException     => (400, "Validation failed"),
            KeyNotFoundException    => (404, "Resource not found"),
            InvalidOperationException => (409, "Conflict"),
            _                       => (500, "An unexpected error occurred")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Detail = exception.Message
        };

        // For validation errors, include field-level details
        if (exception is ValidationException validationEx)
        {
            problem.Extensions["errors"] = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}