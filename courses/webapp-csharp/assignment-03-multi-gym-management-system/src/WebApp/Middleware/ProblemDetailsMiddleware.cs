using System.Net;
using System.Text.Json;
using App.BLL.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Middleware;

public class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (!ShouldHandleAsProblemDetails(context.Request))
            {
                throw;
            }

            logger.LogError(exception, "Unhandled exception");
            await WriteProblemAsync(context, exception);
        }
    }

    private static bool ShouldHandleAsProblemDetails(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var acceptHeader = request.Headers.Accept.ToString();
        return acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
               acceptHeader.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => ((int)HttpStatusCode.NotFound, "Not Found"),
            ForbiddenException => ((int)HttpStatusCode.Forbidden, "Forbidden"),
            ValidationAppException => ((int)HttpStatusCode.BadRequest, "Validation Failed"),
            _ => ((int)HttpStatusCode.InternalServerError, "Server Error")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json; charset=utf-8";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        if (exception is ValidationAppException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
