using EventService.Application.Abstractions.Persistence.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions.Persistence;
using Shared.Contracts.Events;

namespace EventService.UnitTests;

public class BookingConfirmedHandlerTests : IDisposable
{
    private readonly TestServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IBookingConfirmedHandler _handler;
    private readonly IEventRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public BookingConfirmedHandlerTests()
    {
        _provider = new TestServiceProvider();
        _scope = _provider.CreateScope();

        _handler = _scope.ServiceProvider.GetRequiredService<IBookingConfirmedHandler>();
        _repository = _scope.ServiceProvider.GetRequiredService<IEventRepository>();
        _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private async Task<Event> SeedEventAsync(int totalSeats = 10, int? availableSeats = null)
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

        await _repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        return entity;
    }

    private static BookingConfirmed Message(Guid eventId, Guid? bookingId = null, int seats = 1) =>
        new(bookingId ?? Guid.NewGuid(), eventId, Guid.NewGuid(), seats, DateTime.UtcNow);

    private async Task<int> GetAvailableSeatsAsync(Guid eventId)
    {
        var entity = await _repository.GetByIdAsync(eventId, TestContext.Current.CancellationToken);
        return entity!.AvailableSeats;
    }

    [Fact]
    public async Task Handle_ValidMessage_DecreasesAvailableSeats()
    {
        // Arrange
        var entity = await SeedEventAsync(totalSeats: 10);

        // Act
        await _handler.HandleAsync(Message(entity.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(9, await GetAvailableSeatsAsync(entity.Id));
    }

    [Fact]
    public async Task Handle_MessageWithSeveralSeats_DecreasesBySeatsCount()
    {
        // Arrange
        var entity = await SeedEventAsync(totalSeats: 10);

        // Act
        await _handler.HandleAsync(Message(entity.Id, seats: 3), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(7, await GetAvailableSeatsAsync(entity.Id));
    }

    [Fact]
    public async Task Handle_SameMessageDeliveredTwice_DecreasesSeatsOnlyOnce()
    {
        // Arrange
        var entity = await SeedEventAsync(totalSeats: 10);
        var message = Message(entity.Id);

        // Act — Kafka доставляет «минимум один раз», сообщение может прийти повторно
        await _handler.HandleAsync(message, TestContext.Current.CancellationToken);
        await _handler.HandleAsync(message, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(9, await GetAvailableSeatsAsync(entity.Id));
    }

    [Fact]
    public async Task Handle_DifferentBookings_EachDecreasesSeats()
    {
        // Arrange
        var entity = await SeedEventAsync(totalSeats: 10);

        // Act
        await _handler.HandleAsync(Message(entity.Id), TestContext.Current.CancellationToken);
        await _handler.HandleAsync(Message(entity.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(8, await GetAvailableSeatsAsync(entity.Id));
    }

    [Fact]
    public async Task Handle_EventNotFound_SkipsWithoutThrowing()
    {
        // Act
        var exception = await Record.ExceptionAsync(() =>
            _handler.HandleAsync(Message(Guid.NewGuid()), TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Handle_NoAvailableSeats_SkipsAndKeepsSeatsUnchanged()
    {
        // Arrange
        var entity = await SeedEventAsync(totalSeats: 5, availableSeats: 0);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _handler.HandleAsync(Message(entity.Id), TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(exception);
        Assert.Equal(0, await GetAvailableSeatsAsync(entity.Id));
    }

    [Fact]
    public async Task Handle_NotEnoughSeatsForBooking_SkipsAndKeepsSeatsUnchanged()
    {
        // Arrange
        var entity = await SeedEventAsync(totalSeats: 5, availableSeats: 2);

        // Act
        await _handler.HandleAsync(Message(entity.Id, seats: 3), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, await GetAvailableSeatsAsync(entity.Id));
    }

    [Fact]
    public async Task Handle_NonPositiveSeats_SkipsAndKeepsSeatsUnchanged()
    {
        // Arrange
        var entity = await SeedEventAsync(totalSeats: 5);

        // Act
        await _handler.HandleAsync(Message(entity.Id, seats: 0), TestContext.Current.CancellationToken);
        await _handler.HandleAsync(Message(entity.Id, seats: -2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, await GetAvailableSeatsAsync(entity.Id));
    }
}
