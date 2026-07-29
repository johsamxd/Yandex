using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Results;
using ValidationException = Yandex.Application.Exceptions.ValidationException;

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
        
        throw new ValidationException(message);
    }
}