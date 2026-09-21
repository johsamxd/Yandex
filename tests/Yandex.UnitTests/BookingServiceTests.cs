using System.Collections.Concurrent;
using AutoMapper;
using Moq;
using Yandex.Application.Dtos.Bookings;
using Yandex.Application.Exceptions;
using Yandex.Application.Exceptions.Booking;
using Yandex.Application.Services;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;

namespace Yandex.UnitTests;

public class BookingServiceTests
{
    private readonly Mock<IEntityRepository<Booking>> _bookingRepositoryMock;
    private readonly Mock<IEntityRepository<Event>> _eventRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _bookingRepositoryMock = new Mock<IEntityRepository<Booking>>();
        _eventRepositoryMock = new Mock<IEntityRepository<Event>>();
        _mapperMock = new Mock<IMapper>();
        _service = new BookingService(
            _bookingRepositoryMock.Object,
            _eventRepositoryMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task CreateBookingAsync_WithValidEventId_ShouldReturnBookingWithPendingStatus()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 100,
            AvailableSeats = 100
        };

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        _mapperMock.Setup(x => x.Map<BookingDto>(It.IsAny<Booking>()))
            .Returns<Booking>(b => new BookingDto(
                b.Id,
                b.EventId,
                b.Status,
                b.CreatedAt,
                null
            ));

        // Act
        var result = await _service.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(eventId, result.EventId);
        Assert.NotEqual(Guid.Empty, result.Id);

        _eventRepositoryMock.Verify(x => x.GetById(eventId), Times.Once);
        _bookingRepositoryMock.Verify(x => x.Add(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldCreateUniqueIds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 100,
            AvailableSeats = 100
        };

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        _mapperMock.Setup(x => x.Map<BookingDto>(It.IsAny<Booking>()))
            .Returns<Booking>(b => new BookingDto(
                b.Id,
                b.EventId,
                b.Status,
                b.CreatedAt,
                null
            ));

        // Act
        var result1 = await _service.CreateBookingAsync(eventId);
        var result2 = await _service.CreateBookingAsync(eventId);
        var result3 = await _service.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        Assert.NotEqual(result1.Id, result2.Id);
        Assert.NotEqual(result1.Id, result3.Id);
        Assert.NotEqual(result2.Id, result3.Id);

        Assert.All([result1, result2, result3],
            r => Assert.Equal(BookingStatus.Pending, r.Status));

        _bookingRepositoryMock.Verify(x => x.Add(It.IsAny<Booking>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithValidId_ShouldReturnCorrectBooking()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var expectedDto = new BookingDto(
            booking.Id,
            booking.EventId,
            booking.Status,
            booking.CreatedAt,
            null
        );

        _bookingRepositoryMock.Setup(x => x.GetById(bookingId)).Returns(booking);
        _mapperMock.Setup(x => x.Map<BookingDto>(booking)).Returns(expectedDto);

        // Act
        var result = await _service.GetBookingByIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);

        _bookingRepositoryMock.Verify(x => x.GetById(bookingId), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterStatusChange_ShouldReflectUpdatedStatus()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAt = DateTime.UtcNow
        };

        var expectedDto = new BookingDto(
            booking.Id,
            booking.EventId,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt
        );

        _bookingRepositoryMock.Setup(x => x.GetById(bookingId)).Returns(booking);
        _mapperMock.Setup(x => x.Map<BookingDto>(booking)).Returns(expectedDto);

        // Act
        var result = await _service.GetBookingByIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterRejection_ShouldReflectRejectedStatus()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Rejected,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAt = DateTime.UtcNow
        };

        var expectedDto = new BookingDto(
            booking.Id,
            booking.EventId,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt
        );

        _bookingRepositoryMock.Setup(x => x.GetById(bookingId)).Returns(booking);
        _mapperMock.Setup(x => x.Map<BookingDto>(booking)).Returns(expectedDto);

        // Act
        var result = await _service.GetBookingByIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal(BookingStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNonExistentEvent_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();
        _eventRepositoryMock.Setup(x => x.GetById(nonExistentEventId)).Returns((Event)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateBookingAsync(nonExistentEventId));

        Assert.Equal($"Event with id: {nonExistentEventId} not found", exception.Message);

        _eventRepositoryMock.Verify(x => x.GetById(nonExistentEventId), Times.Once);
        _bookingRepositoryMock.Verify(x => x.Add(It.IsAny<Booking>()), Times.Never);
        _mapperMock.Verify(x => x.Map<BookingDto>(It.IsAny<Booking>()), Times.Never);
    }

    [Fact(Skip = "Soft delete еще не реализован. После реализации поменять тест")]
    public async Task CreateBookingAsync_WithDeletedEvent_ShouldThrowNotFoundException()
    {
        // Arrange
        var deletedEventId = Guid.NewGuid();
        _eventRepositoryMock.Setup(x => x.GetById(deletedEventId)).Returns((Event)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateBookingAsync(deletedEventId));

        Assert.Equal($"Event with id: {deletedEventId} not found", exception.Message);

        _eventRepositoryMock.Verify(x => x.GetById(deletedEventId), Times.Once);
        _bookingRepositoryMock.Verify(x => x.Add(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentBookingId = Guid.NewGuid();
        _bookingRepositoryMock.Setup(x => x.GetById(nonExistentBookingId)).Returns((Booking)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetBookingByIdAsync(nonExistentBookingId));

        Assert.Equal($"Booking with id: {nonExistentBookingId} not found", exception.Message);

        _bookingRepositoryMock.Verify(x => x.GetById(nonExistentBookingId), Times.Once);
        _mapperMock.Verify(x => x.Map<BookingDto>(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldDecreaseAvailableSeatsByOne()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 100,
            AvailableSeats = 100
        };
        var initialSeats = existingEvent.AvailableSeats;

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        _mapperMock.Setup(x => x.Map<BookingDto>(It.IsAny<Booking>()))
            .Returns<Booking>(b => new BookingDto(
                b.Id,
                b.EventId,
                b.Status,
                b.CreatedAt,
                null
            ));

        // Act
        await _service.CreateBookingAsync(eventId);

        // Assert
        Assert.Equal(initialSeats - 1, existingEvent.AvailableSeats);
        _eventRepositoryMock.Verify(x => x.Update(existingEvent), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_UpToLimit_ShouldSucceedForEachBooking()
    {
        // Arrange
        const int totalSeats = 5;
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats);

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        SetupMapper();

        // Act
        var results = new List<BookingDto>();
        for (var i = 0; i < totalSeats; i++)
        {
            results.Add(await _service.CreateBookingAsync(eventId));
        }

        // Assert
        Assert.Equal(totalSeats, results.Count);
        Assert.Equal(totalSeats, results.Select(r => r.Id).Distinct().Count());
        Assert.Equal(0, existingEvent.AvailableSeats);
        Assert.All(results, r => Assert.Equal(BookingStatus.Pending, r.Status));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSeatsExhausted_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        const int totalSeats = 2;
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats);

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        SetupMapper();

        await _service.CreateBookingAsync(eventId);
        await _service.CreateBookingAsync(eventId);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _service.CreateBookingAsync(eventId));

        Assert.Equal(0, existingEvent.AvailableSeats);
        _bookingRepositoryMock.Verify(x => x.Add(It.IsAny<Booking>()), Times.Exactly(totalSeats));
    }

    [Fact]
    public async Task CreateBookingAsync_WithZeroAvailableSeats_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats: 1);
        existingEvent.AvailableSeats = 0;

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _service.CreateBookingAsync(eventId));

        _bookingRepositoryMock.Verify(x => x.Add(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public void Confirm_ShouldSetStatusToConfirmedAndFillProcessedAt()
    {
        // Arrange
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Reject_ShouldSetStatusToRejectedAndFillProcessedAt()
    {
        // Arrange
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void ReleaseSeats_AfterReject_ShouldRestoreAvailableSeats()
    {
        // Arrange
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

        // Act
        booking.Reject();
        existingEvent.ReleaseSeats();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.Equal(5, existingEvent.AvailableSeats);
    }

    [Fact]
    public async Task ReleaseSeats_AfterReject_ShouldAllowNewBooking()
    {
        // Arrange
        const int totalSeats = 1;
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats);

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        SetupMapper();

        // Первая бронь занимает единственное место
        var firstBooking = await _service.CreateBookingAsync(eventId);
        Assert.Equal(0, existingEvent.AvailableSeats);

        // Мест больше нет
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _service.CreateBookingAsync(eventId));

        // Освобождаем место (имитация Reject + ReleaseSeats)
        existingEvent.ReleaseSeats();

        // Act
        var secondBooking = await _service.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(secondBooking);
        Assert.NotEqual(firstBooking.Id, secondBooking.Id);
        Assert.Equal(0, existingEvent.AvailableSeats);
        Assert.Equal(BookingStatus.Pending, secondBooking.Status);
    }
    
    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_ShouldNotOverbook()
    {
        // Arrange
        const int totalSeats = 5;
        const int concurrentRequests = 20;
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats);

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        SetupMapper();

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await _service.CreateBookingAsync(eventId);
                    return (Success: true, Exception: (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (Success: false, Exception: ex);
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.Success);
        var noSeatsCount = results.Count(r => r.Exception is NoAvailableSeatsException);
        var otherErrors = results
            .Where(r => !r.Success && r.Exception is not NoAvailableSeatsException)
            .Select(r => r.Exception!)
            .ToList();

        Assert.Empty(otherErrors);
        Assert.Equal(totalSeats, successCount);
        Assert.Equal(concurrentRequests - totalSeats, noSeatsCount);
        Assert.Equal(0, existingEvent.AvailableSeats);

        _bookingRepositoryMock.Verify(
            x => x.Add(It.IsAny<Booking>()),
            Times.Exactly(totalSeats));
    }
    
    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_ShouldProduceUniqueIds()
    {
        // Arrange
        const int totalSeats = 10;
        const int concurrentRequests = 10;
        var eventId = Guid.NewGuid();
        var existingEvent = CreateTestEvent(eventId, totalSeats);

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        SetupMapper();

        var createdBookings = new ConcurrentBag<BookingDto>();

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                var dto = await _service.CreateBookingAsync(eventId);
                createdBookings.Add(dto);
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentRequests, createdBookings.Count);
        Assert.Equal(concurrentRequests,
            createdBookings.Select(b => b.Id).Distinct().Count());
        Assert.Equal(0, existingEvent.AvailableSeats);
    }

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

    private void SetupMapper()
    {
        _mapperMock
            .Setup(x => x.Map<BookingDto>(It.IsAny<Booking>()))
            .Returns<Booking>(b => new BookingDto(
                b.Id,
                b.EventId,
                b.Status,
                b.CreatedAt,
                b.ProcessedAt));
    }
}