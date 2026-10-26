using Yandex.Application.Dtos.Bookings;

namespace Yandex.Application.Abstractions;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<BookingDto> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}