using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yandex.Application.Exceptions;
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

        var tasks = pendingBookings.Select(booking =>
            ProcessBookingAsync(booking, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task ProcessBookingAsync(
        Booking booking,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IEntityRepository<Booking>>();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEntityRepository<Event>>();

        await Task.Delay(_processingDelay, cancellationToken);
        await _processingSemaphore.WaitAsync(cancellationToken);

        try
        {
            logger.LogDebug("Processing booking {BookingId} for event {EventId}",
                booking.Id, booking.EventId);

            var existingEvent = eventRepository.GetById(booking.EventId);
            if (existingEvent == null)
            {
                booking.Reject();
                bookingRepository.Update(booking);
                logger.LogWarning("Event with id {EventId} was not found", booking.EventId);
                return;
            }

            booking.Confirm();
            bookingRepository.Update(booking);

            logger.LogInformation("Booking {BookingId} confirmed at {ProcessedAt}",
                booking.Id, booking.ProcessedAt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing booking {BookingId}", booking.Id);
            booking.Reject();
            bookingRepository.Update(booking);

            var existingEvent = eventRepository.GetById(booking.EventId);
            existingEvent?.ReleaseSeats();
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}