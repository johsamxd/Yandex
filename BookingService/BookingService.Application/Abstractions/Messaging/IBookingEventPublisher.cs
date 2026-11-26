using Shared.Contracts.Events;

namespace BookingService.Application.Abstractions.Messaging;

/// <summary>
/// Публикация событий сервиса Bookings. Абстракция живёт в Application,
/// реализация на Kafka — в Infrastructure.
/// </summary>
public interface IBookingEventPublisher
{
    Task PublishBookingConfirmedAsync(BookingConfirmed message, CancellationToken cancellationToken = default);
}
