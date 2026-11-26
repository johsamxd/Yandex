using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Dtos;
using Shared.Domain.Exceptions;

namespace Shared.Presentation.Middlewares;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    JsonSerializerOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex.StatusCode, ex.Title, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, HttpStatusCode.InternalServerError,
                "Internal server error",
                "An unexpected error occurred. Please try again later.");
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        var response = new ErrorResponse(
            $"https://httpstatuses.com/{(int)statusCode}",
            title,
            (int)statusCode,
            detail,
            context.Request.Path);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}