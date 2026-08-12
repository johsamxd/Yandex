using Yandex.Application.Dtos.Bookings;

namespace Yandex.Application.Abstractions;

public interface IBookingService
{
    BookingDto CreateBookingAsync(Guid eventId);
    BookingDto GetBookingByIdAsync(Guid bookingId);
}