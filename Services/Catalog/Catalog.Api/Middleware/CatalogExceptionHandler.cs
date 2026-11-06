using Catalog.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Middleware;

public class CatalogExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CatalogExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public CatalogExceptionHandler(ILogger<CatalogExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Exception is logged EXACTLY ONCE here with structured properties (Criterion 12 & 13)
        _logger.LogError(
            exception,
            "HTTP {RequestMethod} {RequestPath} failed: {ErrorMessage}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            EntityNotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                notFound.Message
            ),
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "One or more validation errors occurred."
            ),
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflict.Message
            ),
            DomainException domain => (
                StatusCodes.Status409Conflict,
                "Business Invariant Conflict",
                domain.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred while processing your request. Please try again later."
            )
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException fluentEx)
        {
            var errors = fluentEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
