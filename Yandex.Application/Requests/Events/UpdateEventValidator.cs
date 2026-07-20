using FluentValidation;

namespace Yandex.Application.Requests.Events;

public class UpdateEventValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty();

        RuleFor(x => x.StartAt)
            .NotEmpty();

        RuleFor(x => x.EndAt)
            .NotEmpty()
            .GreaterThan(x => x.StartAt);
    }
}