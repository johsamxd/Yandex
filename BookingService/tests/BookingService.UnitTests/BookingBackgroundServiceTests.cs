using System.Collections.Concurrent;
using BookingService.Application.Abstractions.Messaging;
using BookingService.Application.Abstractions.Persistence.Repositories;
using BookingService.Application.Abstractions.Services;
using BookingService.Application.Options;
using BookingService.Application.Services;
using BookingService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts.Events;

namespace BookingService.UnitTests;

/// <summary>
/// Проверяют порядок «сначала сохранить статус, потом опубликовать событие» и содержимое сообщения.
/// Фоновый сервис опрашивает базу раз в секунду, поэтому тесты ждут результата с запасом по времени.
/// </summary>
public class BookingBackgroundServiceTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    private readonly TestServiceProvider _provider = new();
    private readonly IServiceScope _scope;
    private readonly IBookingService _service;

    public BookingBackgroundServiceTests()
    {
        _scope = _provider.CreateScope();
        _service = _scope.ServiceProvider.GetRequiredService<IBookingService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private BookingBackgroundService CreateBackgroundService(IBookingEventPublisher publisher) =>
        new(_provider.ScopeFactory, publisher, NullLogger<BookingBackgroundService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new BookingBackgroundOptions { PollingIntervalSeconds = 1, ProcessingDelaySeconds = 0 }));

    /// <summary>
    /// Читаем в НОВОМ scope: DbContext теста уже отслеживает созданную бронь и вернул бы устаревший статус Pending.
    /// </summary>
    private async Task<BookingStatus> GetStatusFromDbAsync(Guid bookingId)
    {
        using var scope = _provider.CreateScope();
        var booking = await scope.ServiceProvider.GetRequiredService<IBookingRepository>()
            .GetByIdAsync(bookingId, TestContext.Current.CancellationToken);
        return booking!.Status;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Condition was not met in time");
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task PendingBooking_IsConfirmed_AndBookingConfirmedPublished()
    {
        // Arrange
        var publisher = new FakePublisher(_provider.ScopeFactory);
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var booking = await _service.CreateBookingAsync(eventId, userId, TestContext.Current.CancellationToken);
        var background = CreateBackgroundService(publisher);

        // Act
        await background.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => publisher.Messages.Count > 0);
        await background.StopAsync(TestContext.Current.CancellationToken);

        // Assert — сообщение соответствует брони
        var message = Assert.Single(publisher.Messages);
        Assert.Equal(booking.Id, message.BookingId);
        Assert.Equal(eventId, message.EventId);
        Assert.Equal(userId, message.UserId);
        Assert.Equal(1, message.Seats);

        // Assert — статус уже сохранён в базе Bookings
        Assert.Equal(BookingStatus.Confirmed, await GetStatusFromDbAsync(booking.Id));
        Assert.Equal(BookingStatus.Confirmed, publisher.StatusSeenAtPublish[booking.Id]);
    }

    [Fact]
    public async Task PublishFails_BookingStaysConfirmed_NotRejected()
    {
        // Arrange
        var publisher = new FakePublisher(_provider.ScopeFactory) { AlwaysFail = true };
        var booking = await _service.CreateBookingAsync(
            Guid.NewGuid(), Guid.NewGuid(), TestContext.Current.CancellationToken);
        var background = CreateBackgroundService(publisher);

        // Act — ждём все три попытки публикации
        await background.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => publisher.Attempts >= 3);
        await background.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, await GetStatusFromDbAsync(booking.Id));
        Assert.Empty(publisher.Messages);
    }

    private sealed class FakePublisher(IServiceScopeFactory scopeFactory) : IBookingEventPublisher
    {
        private int _attempts;

        public bool AlwaysFail { get; init; }
        public ConcurrentQueue<BookingConfirmed> Messages { get; } = new();
        public ConcurrentDictionary<Guid, BookingStatus> StatusSeenAtPublish { get; } = new();
        public int Attempts => Volatile.Read(ref _attempts);

        public async Task PublishBookingConfirmedAsync(BookingConfirmed message,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _attempts);
            if (AlwaysFail)
            {
                throw new InvalidOperationException("Kafka is down");
            }

            // Что лежит в базе в момент публикации: статус должен быть уже сохранён
            using var scope = scopeFactory.CreateScope();
            var booking = await scope.ServiceProvider.GetRequiredService<IBookingRepository>()
                .GetByIdAsync(message.BookingId, cancellationToken);
            StatusSeenAtPublish[message.BookingId] = booking!.Status;

            Messages.Enqueue(message);
        }
    }
}
