using Yandex.Application.Dtos.Bookings;

namespace Yandex.Application.Abstractions;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId);
    Task<BookingDto> GetBookingByIdAsync(Guid bookingId);
}