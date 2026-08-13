using Yandex.Domain.Enums;

namespace Yandex.Application.Dtos.Bookings;

public record BookingDto(
    Guid Id,
    Guid EventId,
    BookingStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);