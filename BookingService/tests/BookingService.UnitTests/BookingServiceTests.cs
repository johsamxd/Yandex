using BookingService.Application.Abstractions.Persistence.Repositories;
using BookingService.Application.Abstractions.Services;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions.Booking;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Exceptions;

namespace BookingService.UnitTests;

/// <summary>
/// Бронь знает о событии и пользователе только по идентификаторам — самих событий в базе Bookings нет.
/// </summary>
public class BookingServiceTests : IDisposable
{
    private readonly TestServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IBookingService _service;
    private readonly IBookingRepository _repository;

    public BookingServiceTests()
    {
        _provider = new TestServiceProvider();
        _scope = _provider.CreateScope();

        _service = _scope.ServiceProvider.GetRequiredService<IBookingService>();
        _repository = _scope.ServiceProvider.GetRequiredService<IBookingRepository>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ===== Create =====

    [Fact]
    public async Task CreateBookingAsync_ReturnsPendingBooking_AndSavesIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Act
        var result = await _service.CreateBookingAsync(eventId, userId, Ct);

        // Assert — DTO
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(userId, result.UserId);

        // Assert — реально сохранилось
        var saved = await _repository.GetByIdAsync(result.Id, Ct);
        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_SameEventTwice_CreatesTwoBookings()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var first = await _service.CreateBookingAsync(eventId, userId, Ct);
        var second = await _service.CreateBookingAsync(eventId, userId, Ct);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_TenActiveBookings_AllowedButEleventhRejected()
    {
        // Arrange
        var userId = Guid.NewGuid();
        for (var i = 0; i < 10; i++)
        {
            await _service.CreateBookingAsync(Guid.NewGuid(), userId, Ct);
        }

        // Act + Assert
        await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            _service.CreateBookingAsync(Guid.NewGuid(), userId, Ct));
    }

    [Fact]
    public async Task CreateBookingAsync_LimitIsPerUser()
    {
        var firstUser = Guid.NewGuid();
        for (var i = 0; i < 10; i++)
        {
            await _service.CreateBookingAsync(Guid.NewGuid(), firstUser, Ct);
        }

        var result = await _service.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid(), Ct);

        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_CancelledBookingsDoNotCountTowardLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var first = await _service.CreateBookingAsync(Guid.NewGuid(), userId, Ct);
        for (var i = 0; i < 9; i++)
        {
            await _service.CreateBookingAsync(Guid.NewGuid(), userId, Ct);
        }

        // Act — освобождаем один слот
        await _service.CancelBookingAsync(first.Id, userId, isAdmin: false, Ct);
        var result = await _service.CreateBookingAsync(Guid.NewGuid(), userId, Ct);

        // Assert
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    // ===== Cancel =====

    [Fact]
    public async Task CancelBookingAsync_OwnBooking_SetsCancelled()
    {
        var userId = Guid.NewGuid();
        var booking = await _service.CreateBookingAsync(Guid.NewGuid(), userId, Ct);

        var result = await _service.CancelBookingAsync(booking.Id, userId, isAdmin: false, Ct);

        Assert.Equal(BookingStatus.Cancelled, result.Status);
        Assert.Equal(BookingStatus.Cancelled, (await _repository.GetByIdAsync(booking.Id, Ct))!.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_AlreadyCancelled_IsIdempotent()
    {
        var userId = Guid.NewGuid();
        var booking = await _service.CreateBookingAsync(Guid.NewGuid(), userId, Ct);
        await _service.CancelBookingAsync(booking.Id, userId, isAdmin: false, Ct);

        var result = await _service.CancelBookingAsync(booking.Id, userId, isAdmin: false, Ct);

        Assert.Equal(BookingStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_SomeoneElsesBooking_ThrowsForbidden_AndKeepsStatus()
    {
        var booking = await _service.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid(), Ct);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.CancelBookingAsync(booking.Id, Guid.NewGuid(), isAdmin: false, Ct));

        Assert.Equal(BookingStatus.Pending, (await _repository.GetByIdAsync(booking.Id, Ct))!.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_AdminCancelsSomeoneElsesBooking_Succeeds()
    {
        var booking = await _service.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid(), Ct);

        var result = await _service.CancelBookingAsync(booking.Id, Guid.NewGuid(), isAdmin: true, Ct);

        Assert.Equal(BookingStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_NotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CancelBookingAsync(Guid.NewGuid(), Guid.NewGuid(), isAdmin: false, Ct));
    }

    // ===== Get =====

    [Fact]
    public async Task GetBookingByIdAsync_OwnBooking_ReturnsIt()
    {
        var userId = Guid.NewGuid();
        var booking = await _service.CreateBookingAsync(Guid.NewGuid(), userId, Ct);

        var result = await _service.GetBookingByIdAsync(booking.Id, userId, isAdmin: false, Ct);

        Assert.Equal(booking.Id, result.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_SomeoneElsesBooking_ThrowsForbidden()
    {
        var booking = await _service.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid(), Ct);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.GetBookingByIdAsync(booking.Id, Guid.NewGuid(), isAdmin: false, Ct));
    }

    [Fact]
    public async Task GetBookingByIdAsync_Admin_CanReadAnyBooking()
    {
        var booking = await _service.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid(), Ct);

        var result = await _service.GetBookingByIdAsync(booking.Id, Guid.NewGuid(), isAdmin: true, Ct);

        Assert.Equal(booking.Id, result.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetBookingByIdAsync(Guid.NewGuid(), Guid.NewGuid(), isAdmin: true, Ct));
    }
}
