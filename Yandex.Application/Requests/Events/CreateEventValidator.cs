using FluentValidation;

namespace Yandex.Application.Requests.Events;

public class CreateEventValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty();

        RuleFor(x => x.StartAt)
            .NotEmpty()
            .GreaterThan(DateTime.UtcNow);

        RuleFor(x => x.EndAt)
            .NotEmpty()
            .GreaterThan(x => x.StartAt);
    }
}