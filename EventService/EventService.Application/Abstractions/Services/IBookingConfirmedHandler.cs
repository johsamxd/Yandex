using Shared.Contracts.Events;

namespace EventService.Application.Abstractions.Services;

/// <summary>
/// Обработка события <see cref="BookingConfirmed"/>, пришедшего из Kafka.
/// </summary>
public interface IBookingConfirmedHandler
{
    Task HandleAsync(BookingConfirmed message, CancellationToken cancellationToken = default);
}
