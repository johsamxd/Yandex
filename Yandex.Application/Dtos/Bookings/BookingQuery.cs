using Yandex.Domain.Enums;

namespace Yandex.Application.Dtos.Bookings;

public record BookingQuery(
    BookingStatus? Status = null
);