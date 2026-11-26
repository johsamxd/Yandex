using BookingService.Domain.Enums;

namespace BookingService.Application.Dtos.Bookings;

public record BookingDto(
    Guid Id,
    Guid EventId,
    Guid UserId,
    BookingStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);