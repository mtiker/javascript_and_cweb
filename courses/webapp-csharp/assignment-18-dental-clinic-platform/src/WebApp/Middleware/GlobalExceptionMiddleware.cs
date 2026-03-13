using System.Net;
using App.BLL.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private static Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ValidationAppException => (HttpStatusCode.BadRequest, "Validation failed"),
            NotFoundException => (HttpStatusCode.NotFound, "Resource was not found"),
            ForbiddenException => (HttpStatusCode.Forbidden, "Access denied"),
            _ => (HttpStatusCode.InternalServerError, "Server error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Extensions = { ["traceId"] = context.TraceIdentifier }
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
