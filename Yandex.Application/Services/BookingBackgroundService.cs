using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yandex.Application.Abstractions.Persistence;
using Yandex.Application.Abstractions.Persistence.Repositories;
using Yandex.Application.Dtos.Bookings;
using Yandex.Application.Options;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;

namespace Yandex.Application.Services;

public class BookingBackgroundService(
    IServiceScopeFactory scopeFactory,
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
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await Task.Delay(_processingDelay, cancellationToken);
        await _processingSemaphore.WaitAsync(cancellationToken);

        try
        {
            logger.LogDebug("Processing booking {BookingId} for event {EventId}",
                booking.Id, booking.EventId);

            var existingEvent = await eventRepository.GetByIdAsync(booking.EventId, cancellationToken);
            if (existingEvent == null)
            {
                booking.Reject();
                await bookingRepository.UpdateAsync(booking, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogWarning("Event with id {EventId} was not found", booking.EventId);
                return;
            }

            booking.Confirm();
            await bookingRepository.UpdateAsync(booking, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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
            await bookingRepository.UpdateAsync(booking, cancellationToken);

            var existingEvent = await eventRepository.GetByIdAsync(booking.EventId, cancellationToken);
            existingEvent?.ReleaseSeats();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}