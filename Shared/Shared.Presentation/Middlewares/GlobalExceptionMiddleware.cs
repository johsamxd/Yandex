using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Dtos;
using Shared.Domain.Exceptions;

namespace Shared.Presentation.Middlewares;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, JsonSerializerOptions options)
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
            ForbiddenException => (HttpStatusCode.Forbidden, "Forbidden", exception.Message),
            ConflictException => (HttpStatusCode.Conflict, "Conflict", exception.Message),
            UnauthorizedException => (HttpStatusCode.Unauthorized, "Unauthorized", exception.Message)
        };

        var response = new ErrorResponse(
            $"https://httpstatuses.com/{(int)statusCode}",
            title,
            (int)statusCode,
            detail,
            context.Request.Path
        );

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}