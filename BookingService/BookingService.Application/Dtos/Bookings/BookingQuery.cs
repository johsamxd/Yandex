using BookingService.Domain.Enums;

namespace BookingService.Application.Dtos.Bookings;

public record BookingQuery(
    BookingStatus? Status = null
);