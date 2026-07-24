using System.Net;
using System.Text.Json;
using Yandex.Application;
using Yandex.Application.Exceptions;
using Yandex.Application.Models;

namespace Yandex.Web.Middlewares;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found", exception.Message),
            ValidationException => (HttpStatusCode.BadRequest, "Validation error", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "Internal server error",
                "An unexpected error occurred. Please try again later.")
        };

        var response = new ErrorResponse(
            $"https://httpstatuses.com/{(int)statusCode}",
            title,
            (int)statusCode,
            detail,
            context.Request.Path
        );
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}