using FluentValidation;
using Yandex.Application.Dtos.Events;

namespace Yandex.Application.Requests.Events;

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
            .Must(x => !(x.StartAt.HasValue && x.EndAt.HasValue && x.StartAt > x.EndAt));
    }
}