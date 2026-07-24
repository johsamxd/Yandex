using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Results;
using Yandex.Application.Models;

namespace Yandex.Application.FluentValidation;

public class FluentValidationResponseFactory : IFluentValidationAutoValidationResultFactory
{
    public Task<IActionResult?> CreateActionResult(ActionExecutingContext context,
        ValidationProblemDetails validationProblemDetails,
        IDictionary<IValidationContext, ValidationResult> validationResults)
    {
        var errors = validationResults
            .SelectMany(x => x.Value.Errors)
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage).ToArray()
            );
        var errorMessages = errors
            .Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}")
            .ToList();
        var message = errorMessages.Any()
            ? $"{string.Join("; ", errorMessages)}"
            : "Validation failed";
        var response = new ErrorResponse(
            $"https://httpstatuses.com/{(int)HttpStatusCode.BadRequest}",
            "Validation error",
            (int)HttpStatusCode.BadRequest,
            message,
            context.HttpContext.Request.Path
        );
        
        return Task.FromResult<IActionResult?>(
            new ObjectResult(response)
            {
                StatusCode = (int)HttpStatusCode.BadRequest
            }
        );
    }
}