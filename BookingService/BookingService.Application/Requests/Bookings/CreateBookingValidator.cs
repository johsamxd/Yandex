using FluentValidation;

namespace BookingService.Application.Requests.Bookings;

public class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
    }
}
