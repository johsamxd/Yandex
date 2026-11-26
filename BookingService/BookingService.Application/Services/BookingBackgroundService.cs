using BookingService.Application.Abstractions.Messaging;
using BookingService.Application.Abstractions.Persistence.Repositories;
using BookingService.Application.Dtos.Bookings;
using BookingService.Application.Options;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Application.Abstractions.Persistence;
using Shared.Contracts.Events;

namespace BookingService.Application.Services;

public class BookingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IBookingEventPublisher publisher,
    ILogger<BookingBackgroundService> logger,
    IOptions<BookingBackgroundOptions> options) : BackgroundService
{
    // Одна бронь занимает одно место
    private const int SeatsPerBooking = 1;
    private const int PublishAttempts = 3;

    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);
    private readonly TimeSpan _processingDelay = TimeSpan.FromSeconds(options.Value.ProcessingDelaySeconds);
    private readonly SemaphoreSlim _processingSemaphore = new(3, 3);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("BookingBackgroundService started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBookingsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing pending bookings");
            }

            await Task.Delay(_pollingInterval, cancellationToken);
        }

        logger.LogInformation("BookingBackgroundService stopped");
    }

    private async Task ProcessPendingBookingsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var pendingBookings = await bookingRepository.GetFilteredAsync(new BookingQuery()
        {
            Status = BookingStatus.Pending
        }, cancellationToken);

        if (pendingBookings.Count == 0)
        {
            return;
        }

        logger.LogInformation("Found {Count} pending bookings to process", pendingBookings.Count);

        var tasks = pendingBookings.Select(booking =>
            ProcessBookingAsync(booking, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task ProcessBookingAsync(
        Booking booking,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await Task.Delay(_processingDelay, cancellationToken);
        await _processingSemaphore.WaitAsync(cancellationToken);

        BookingConfirmed? confirmed = null;

        try
        {
            logger.LogDebug("Processing booking {BookingId} for event {EventId}",
                booking.Id, booking.EventId);

            // 1. Сначала фиксируем новый статус в СВОЕЙ базе
            booking.Confirm();
            await bookingRepository.UpdateAsync(booking, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Booking {BookingId} confirmed at {ProcessedAt}",
                booking.Id, booking.ProcessedAt);

            confirmed = new BookingConfirmed(
                booking.Id,
                booking.EventId,
                booking.UserId,
                SeatsPerBooking,
                booking.ProcessedAt ?? DateTime.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Cancellation requested");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing booking {BookingId}", booking.Id);
            booking.Reject();
            await bookingRepository.UpdateAsync(booking, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _processingSemaphore.Release();
        }

        // 2. И только потом публикуем событие в Kafka. Сервис Events уменьшит количество мест сам.
        // Публикуем после освобождения семафора, чтобы сетевой вызов не занимал слот обработки.
        if (confirmed is not null)
        {
            await PublishConfirmedAsync(confirmed, cancellationToken);
        }
    }

    private async Task PublishConfirmedAsync(BookingConfirmed message, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= PublishAttempts; attempt++)
        {
            try
            {
                await publisher.PublishBookingConfirmedAsync(message, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Publishing BookingConfirmed for booking {BookingId} was cancelled", message.BookingId);
                return;
            }
            catch (Exception ex)
            {
                // Бронь уже подтверждена и сохранена — статус НЕ откатываем (иначе получим рассинхрон с Kafka)
                logger.LogError(ex,
                    "Failed to publish BookingConfirmed for booking {BookingId} (attempt {Attempt}/{Max})",
                    message.BookingId, attempt, PublishAttempts);

                if (attempt < PublishAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
                }
            }
        }
    }
}
