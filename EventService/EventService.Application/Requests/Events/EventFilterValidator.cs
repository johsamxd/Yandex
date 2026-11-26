using EventService.Application.Dtos.Events;
using FluentValidation;

namespace EventService.Application.Requests.Events;

public class EventFilterValidator : AbstractValidator<EventFilter>
{
    public EventFilterValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(x => x)
            .Must(x => !(x.From.HasValue && x.To.HasValue && x.From > x.To));
    }
}