using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yandex.Application.Options;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;

namespace Yandex.Application.Services;

public class BookingBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<BookingBackgroundService> logger,
    IOptions<BookingBackgroundOptions> options) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);
    private readonly TimeSpan _processingDelay = TimeSpan.FromSeconds(options.Value.ProcessingDelaySeconds);

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
        using var scope = serviceProvider.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IEntityRepository<Booking>>();

        var pendingBookings = bookingRepository
            .GetAll()
            .Where(b => b.Status == BookingStatus.Pending)
            .ToList();

        if (pendingBookings.Count == 0)
        {
            return;
        }

        logger.LogInformation("Found {Count} pending bookings to process", pendingBookings.Count);

        foreach (var booking in pendingBookings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Имитация обращения к внешней системе
                logger.LogDebug("Processing booking {BookingId} for event {EventId}",
                    booking.Id, booking.EventId);

                await Task.Delay(_processingDelay, cancellationToken);

                // Переводим бронь в статус Confirmed
                booking.Status = BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.UtcNow;

                bookingRepository.Update(booking);

                logger.LogInformation("Booking {BookingId} confirmed at {ProcessedAt}",
                    booking.Id, booking.ProcessedAt);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing booking {BookingId}", booking.Id);

                booking.Status = BookingStatus.Rejected;
                booking.ProcessedAt = DateTime.UtcNow;
                bookingRepository.Update(booking);
            }
        }
    }
}