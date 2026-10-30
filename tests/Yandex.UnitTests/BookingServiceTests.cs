using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos.Bookings;
using Yandex.Application.Exceptions;
using Yandex.Application.Exceptions.Booking;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;

namespace Yandex.UnitTests;

public class BookingServiceTests : IDisposable
{
    private readonly TestServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IBookingService _service;
    private readonly IEntityRepository<Booking> _bookingRepository;
    private readonly IEntityRepository<Event> _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BookingServiceTests()
    {
        _provider = new TestServiceProvider();
        _scope = _provider.CreateScope();

        _service = _scope.ServiceProvider.GetRequiredService<IBookingService>();
        _bookingRepository = _scope.ServiceProvider.GetRequiredService<IEntityRepository<Booking>>();
        _eventRepository = _scope.ServiceProvider.GetRequiredService<IEntityRepository<Event>>();
        _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private async Task<Event> SeedEventAsync(int totalSeats = 100, int? availableSeats = null)
    {
        var entity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Description = "Test Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats ?? totalSeats
        };

        await _eventRepository.AddAsync(entity, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        return entity;
    }

    // ===== Success scenarios =====

    [Fact]
    public async Task CreateBookingAsync_WithValidEventId_ShouldReturnBookingWithPendingStatus()
    {
        // Arrange
        var seededEvent = await SeedEventAsync();

        // Act
        var result = await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);

        // Assert — DTO
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(seededEvent.Id, result.EventId);
        Assert.NotEqual(Guid.Empty, result.Id);

        // Assert — реально сохранилось
        var fromDb = await _bookingRepository.GetByIdAsync(result.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(fromDb);
        Assert.Equal(BookingStatus.Pending, fromDb.Status);
        Assert.Equal(seededEvent.Id, fromDb.EventId);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldCreateUniqueIds()
    {
        // Arrange
        var seededEvent = await SeedEventAsync();

        // Act
        var result1 = await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);
        var result2 = await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);
        var result3 = await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
        Assert.NotEqual(result1.Id, result3.Id);
        Assert.NotEqual(result2.Id, result3.Id);
        Assert.All(new[] { result1, result2, result3 },
            r => Assert.Equal(BookingStatus.Pending, r.Status));

        // Три брони реально в БД
        var bookings = await _bookingRepository.GetAllAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, bookings.Count);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithValidId_ShouldReturnCorrectBooking()
    {
        // Arrange
        var seededEvent = await SeedEventAsync();
        var created = await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);

        // Act
        var result = await _service.GetBookingByIdAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(seededEvent.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterStatusChange_ShouldReflectUpdatedStatus()
    {
        // Arrange
        var seededEvent = await SeedEventAsync();
        var created = await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);

        var booking = await _bookingRepository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        booking.Confirm();
        await _bookingRepository.UpdateAsync(booking, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.GetBookingByIdAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterRejection_ShouldReflectRejectedStatus()
    {
        // Arrange
        var seededEvent = await SeedEventAsync();
        var created = await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);

        var booking = await _bookingRepository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        booking.Reject();
        await _bookingRepository.UpdateAsync(booking, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.GetBookingByIdAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BookingStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNonExistentEvent_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateBookingAsync(nonExistentEventId, TestContext.Current.CancellationToken));

        Assert.Equal($"Event with id: {nonExistentEventId} not found", exception.Message);

        // Броней в БД нет
        var bookings = await _bookingRepository.GetAllAsync(TestContext.Current.CancellationToken);
        Assert.Empty(bookings);
    }

    [Fact(Skip = "Soft delete еще не реализован. После реализации поменять тест")]
    public async Task CreateBookingAsync_WithDeletedEvent_ShouldThrowNotFoundException()
    {
        var deletedEventId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateBookingAsync(deletedEventId, TestContext.Current.CancellationToken));

        Assert.Equal($"Event with id: {deletedEventId} not found", exception.Message);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentBookingId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetBookingByIdAsync(nonExistentBookingId, TestContext.Current.CancellationToken));

        Assert.Equal($"Booking with id: {nonExistentBookingId} not found", exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldDecreaseAvailableSeatsByOne()
    {
        // Arrange
        var seededEvent = await SeedEventAsync(totalSeats: 100);
        Assert.Equal(100, seededEvent.AvailableSeats);

        // Act
        await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);

        // Assert — читаем из БД, а не из засеянного инстанса
        var fromDb = await _eventRepository.GetByIdAsync(seededEvent.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(fromDb);
        Assert.Equal(99, fromDb.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_UpToLimit_ShouldSucceedForEachBooking()
    {
        // Arrange
        const int totalSeats = 5;
        var seededEvent = await SeedEventAsync(totalSeats: totalSeats);

        // Act
        var results = new List<BookingDto>();
        for (var i = 0; i < totalSeats; i++)
        {
            results.Add(await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken));
        }

        // Assert
        Assert.Equal(totalSeats, results.Count);
        Assert.Equal(totalSeats, results.Select(r => r.Id).Distinct().Count());
        Assert.All(results, r => Assert.Equal(BookingStatus.Pending, r.Status));

        var fromDb = await _eventRepository.GetByIdAsync(seededEvent.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(fromDb);
        Assert.Equal(0, fromDb.AvailableSeats);

        var bookings = await _bookingRepository.GetAllAsync(TestContext.Current.CancellationToken);
        Assert.Equal(totalSeats, bookings.Count);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSeatsExhausted_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        const int totalSeats = 2;
        var seededEvent = await SeedEventAsync(totalSeats: totalSeats);

        await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);
        await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken));

        var fromDb = await _eventRepository.GetByIdAsync(seededEvent.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(fromDb);
        Assert.Equal(0, fromDb.AvailableSeats);

        var bookings = await _bookingRepository.GetAllAsync(TestContext.Current.CancellationToken);
        Assert.Equal(totalSeats, bookings.Count);
    }

    [Fact]
    public async Task CreateBookingAsync_WithZeroAvailableSeats_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        var seededEvent = await SeedEventAsync(totalSeats: 1, availableSeats: 0);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken));

        var bookings = await _bookingRepository.GetAllAsync(TestContext.Current.CancellationToken);
        Assert.Empty(bookings);
    }

    // ===== Domain tests (не трогают БД) =====

    [Fact]
    public void Confirm_ShouldSetStatusToConfirmedAndFillProcessedAt()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Reject_ShouldSetStatusToRejectedAndFillProcessedAt()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void ReleaseSeats_AfterReject_ShouldRestoreAvailableSeats()
    {
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 5);

        existingEvent.TryReserveSeats();
        Assert.Equal(4, existingEvent.AvailableSeats);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        booking.Reject();
        existingEvent.ReleaseSeats();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.Equal(5, existingEvent.AvailableSeats);
    }

    [Fact]
    public async Task ReleaseSeats_AfterReject_ShouldAllowNewBooking()
    {
        // Arrange
        const int totalSeats = 1;
        var seededEvent = await SeedEventAsync(totalSeats: totalSeats);

        // Первая бронь занимает единственное место
        var firstBooking = await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);

        var afterFirst = await _eventRepository.GetByIdAsync(seededEvent.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(afterFirst);
        Assert.Equal(0, afterFirst.AvailableSeats);

        // Мест больше нет
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken));

        // Освобождаем место (имитация Reject + ReleaseSeats)
        afterFirst.ReleaseSeats();
        await _eventRepository.UpdateAsync(afterFirst, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var secondBooking = await _service.CreateBookingAsync(seededEvent.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(secondBooking);
        Assert.NotEqual(firstBooking.Id, secondBooking.Id);
        Assert.Equal(BookingStatus.Pending, secondBooking.Status);

        var finalEvent = await _eventRepository.GetByIdAsync(seededEvent.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(finalEvent);
        Assert.Equal(0, finalEvent.AvailableSeats);
    }

    // ===== Конкурентность =====

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_ShouldNotOverbook()
    {
        // Arrange
        const int totalSeats = 5;
        const int concurrentRequests = 20;
        var seededEvent = await SeedEventAsync(totalSeats: totalSeats);
        var eventId = seededEvent.Id;

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _provider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    await bookingService.CreateBookingAsync(eventId);
                    return (Success: true, Exception: null);
                }
                catch (Exception ex)
                {
                    return (Success: false, Exception: ex);
                }
            }));

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.Success);
        var noSeatsCount = results.Count(r => r.Exception is NoAvailableSeatsException);
        var otherErrors = results
            .Where(r => r is { Success: false, Exception: not NoAvailableSeatsException })
            .Select(r => r.Exception!)
            .ToList();

        Assert.Empty(otherErrors);
        Assert.Equal(totalSeats, successCount);
        Assert.Equal(concurrentRequests - totalSeats, noSeatsCount);

        using var assertScope = _provider.CreateScope();
        var eventRepo = assertScope.ServiceProvider.GetRequiredService<IEntityRepository<Event>>();
        var finalEvent = await eventRepo.GetByIdAsync(eventId, TestContext.Current.CancellationToken);
        Assert.NotNull(finalEvent);
        Assert.Equal(0, finalEvent.AvailableSeats);

        var bookingRepo = assertScope.ServiceProvider.GetRequiredService<IEntityRepository<Booking>>();
        var bookings = await bookingRepo.GetAllAsync(TestContext.Current.CancellationToken);
        Assert.Equal(totalSeats, bookings.Count);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_ShouldProduceUniqueIds()
    {
        // Arrange
        const int totalSeats = 10;
        const int concurrentRequests = 10;
        var seededEvent = await SeedEventAsync(totalSeats: totalSeats);
        var eventId = seededEvent.Id;

        var createdBookings = new ConcurrentBag<BookingDto>();

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _provider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var dto = await bookingService.CreateBookingAsync(eventId);
                createdBookings.Add(dto);
            }));

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentRequests, createdBookings.Count);
        Assert.Equal(concurrentRequests,
            createdBookings.Select(b => b.Id).Distinct().Count());

        using var assertScope = _provider.CreateScope();
        var eventRepo = assertScope.ServiceProvider.GetRequiredService<IEntityRepository<Event>>();
        var finalEvent = await eventRepo.GetByIdAsync(eventId, TestContext.Current.CancellationToken);
        Assert.NotNull(finalEvent);
        Assert.Equal(0, finalEvent.AvailableSeats);
    }

    // ===== Helpers =====

    private static Event CreateTestEvent(Guid id, int totalSeats = 100)
    {
        return new Event
        {
            Id = id,
            Title = "Test Event",
            Description = "Test Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }
}