using Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request {Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException => (StatusCodes.Status400BadRequest, "Validation failed", validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray())),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", (IDictionary<string, string[]>)new Dictionary<string, string[]>()),
            BusinessException => (StatusCodes.Status422UnprocessableEntity, "Business rule violation", (IDictionary<string, string[]>)new Dictionary<string, string[]>()),
            InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, "Operation is not valid", (IDictionary<string, string[]>)new Dictionary<string, string[]>()),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", (IDictionary<string, string[]>)new Dictionary<string, string[]>())
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        if (errors.Count != 0)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
