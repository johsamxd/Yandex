using AutoMapper;
using Moq;
using Yandex.Application.Dtos.Bookings;
using Yandex.Application.Exceptions;
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
    public void CreateBookingAsync_WithValidEventId_ShouldReturnBookingWithPendingStatus()
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
        };

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        var expectedDto = new BookingDto(
            booking.Id,
            booking.EventId,
            booking.Status,
            booking.CreatedAt
        );

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        _mapperMock.Setup(x => x.Map<BookingDto>(It.IsAny<Booking>())).Returns(expectedDto);

        // Act
        var result = _service.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(eventId, result.EventId);
        Assert.NotEqual(Guid.Empty, result.Id);

        _eventRepositoryMock.Verify(x => x.GetById(eventId), Times.Once);
        _bookingRepositoryMock.Verify(x => x.Add(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public void CreateBookingAsync_MultipleBookingsForSameEvent_ShouldCreateUniqueIds()
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
        };

        _eventRepositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        _mapperMock.Setup(x => x.Map<BookingDto>(It.IsAny<Booking>()))
            .Returns<Booking>(b => new BookingDto(
                b.Id,
                b.EventId,
                b.Status,
                b.CreatedAt
            ));

        // Act
        var result1 = _service.CreateBookingAsync(eventId);
        var result2 = _service.CreateBookingAsync(eventId);
        var result3 = _service.CreateBookingAsync(eventId);

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
    public void GetBookingByIdAsync_WithValidId_ShouldReturnCorrectBooking()
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
            booking.CreatedAt
        );

        _bookingRepositoryMock.Setup(x => x.GetById(bookingId)).Returns(booking);
        _mapperMock.Setup(x => x.Map<BookingDto>(booking)).Returns(expectedDto);

        // Act
        var result = _service.GetBookingByIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);

        _bookingRepositoryMock.Verify(x => x.GetById(bookingId), Times.Once);
    }

    [Fact]
    public void GetBookingByIdAsync_AfterStatusChange_ShouldReflectUpdatedStatus()
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
            booking.CreatedAt
        );

        _bookingRepositoryMock.Setup(x => x.GetById(bookingId)).Returns(booking);
        _mapperMock.Setup(x => x.Map<BookingDto>(booking)).Returns(expectedDto);

        // Act
        var result = _service.GetBookingByIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
    }
    
    [Fact]
    public void GetBookingByIdAsync_AfterRejection_ShouldReflectRejectedStatus()
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
            booking.CreatedAt
        );

        _bookingRepositoryMock.Setup(x => x.GetById(bookingId)).Returns(booking);
        _mapperMock.Setup(x => x.Map<BookingDto>(booking)).Returns(expectedDto);

        // Act
        var result = _service.GetBookingByIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal(BookingStatus.Rejected, result.Status);
    }
    
    [Fact]
    public void CreateBookingAsync_WithNonExistentEvent_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();
        _eventRepositoryMock.Setup(x => x.GetById(nonExistentEventId)).Returns((Event)null!);

        // Act & Assert
        var exception = Assert.Throws<NotFoundException>(() => 
            _service.CreateBookingAsync(nonExistentEventId));
        
        Assert.Equal($"Event with id: {nonExistentEventId} not found", exception.Message);
        
        _eventRepositoryMock.Verify(x => x.GetById(nonExistentEventId), Times.Once);
        _bookingRepositoryMock.Verify(x => x.Add(It.IsAny<Booking>()), Times.Never);
        _mapperMock.Verify(x => x.Map<BookingDto>(It.IsAny<Booking>()), Times.Never);
    }
    
    /* TODO: Soft delete еще не реализован. После реализации поменять тест */
    [Fact]
    public void CreateBookingAsync_WithDeletedEvent_ShouldThrowNotFoundException()
    {
        // Arrange
        var deletedEventId = Guid.NewGuid();
        _eventRepositoryMock.Setup(x => x.GetById(deletedEventId)).Returns((Event)null!);

        // Act & Assert
        var exception = Assert.Throws<NotFoundException>(() => 
            _service.CreateBookingAsync(deletedEventId));
        
        Assert.Equal($"Event with id: {deletedEventId} not found", exception.Message);
        
        _eventRepositoryMock.Verify(x => x.GetById(deletedEventId), Times.Once);
        _bookingRepositoryMock.Verify(x => x.Add(It.IsAny<Booking>()), Times.Never);
    }
    
    [Fact]
    public void GetBookingByIdAsync_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentBookingId = Guid.NewGuid();
        _bookingRepositoryMock.Setup(x => x.GetById(nonExistentBookingId)).Returns((Booking)null!);

        // Act & Assert
        var exception = Assert.Throws<NotFoundException>(() => 
            _service.GetBookingByIdAsync(nonExistentBookingId));
        
        Assert.Equal($"Booking with id: {nonExistentBookingId} not found", exception.Message);
        
        _bookingRepositoryMock.Verify(x => x.GetById(nonExistentBookingId), Times.Once);
        _mapperMock.Verify(x => x.Map<BookingDto>(It.IsAny<Booking>()), Times.Never);
    }
}