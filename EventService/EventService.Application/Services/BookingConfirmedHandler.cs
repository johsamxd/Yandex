using EventService.Application.Abstractions.Persistence.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.Application.Abstractions.Persistence;
using Shared.Contracts.Events;

namespace EventService.Application.Services;

/// <summary>
/// Уменьшает количество свободных мест у события, когда в Bookings подтверждена бронь.
/// «Плохие» сообщения (событие не найдено, нет мест, повтор) пропускаются с логированием,
/// чтобы подписчик не падал и не застревал на одном сообщении.
/// </summary>
public class BookingConfirmedHandler(
    IEventRepository eventRepository,
    IProcessedBookingRepository processedBookingRepository,
    IUnitOfWork unitOfWork,
    ILogger<BookingConfirmedHandler> logger) : IBookingConfirmedHandler
{
    public async Task HandleAsync(BookingConfirmed message, CancellationToken cancellationToken = default)
    {
        if (message.Seats <= 0)
        {
            logger.LogWarning(
                "Skipping BookingConfirmed for booking {BookingId}: invalid seats count {Seats}",
                message.BookingId, message.Seats);
            return;
        }

        // Идемпотентность: повторная доставка того же сообщения не должна уменьшать места второй раз
        if (await processedBookingRepository.ExistsAsync(message.BookingId, cancellationToken))
        {
            logger.LogInformation(
                "Skipping BookingConfirmed for booking {BookingId}: already processed", message.BookingId);
            return;
        }

        var existingEvent = await eventRepository.GetByIdAsync(message.EventId, cancellationToken);
        if (existingEvent is null)
        {
            logger.LogWarning(
                "Skipping BookingConfirmed for booking {BookingId}: event {EventId} not found",
                message.BookingId, message.EventId);
            return;
        }

        if (!existingEvent.TryReserveSeats(message.Seats))
        {
            logger.LogWarning(
                "Skipping BookingConfirmed for booking {BookingId}: event {EventId} has only {Available} seats, requested {Seats}",
                message.BookingId, message.EventId, existingEvent.AvailableSeats, message.Seats);
            return;
        }

        await processedBookingRepository.AddAsync(new ProcessedBooking
        {
            BookingId = message.BookingId,
            EventId = message.EventId
        }, cancellationToken);

        // Уменьшение мест и отметка «обработано» сохраняются одной транзакцией SaveChanges
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reserved {Seats} seat(s) for event {EventId} (booking {BookingId}); {Available} left",
            message.Seats, message.EventId, message.BookingId, existingEvent.AvailableSeats);
    }
}
