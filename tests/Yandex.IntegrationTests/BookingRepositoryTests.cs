using Microsoft.EntityFrameworkCore;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;
using Yandex.Infrastructure.Persistence;
using Yandex.Infrastructure.Persistence.Repositories;

namespace Yandex.IntegrationTests;

[Collection("Postgres collection")]
public class BookingRepositoryTests(PostgresFixture fixture) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private BookingRepository _repository = null!;

    public async ValueTask InitializeAsync()
    {
        _context = fixture.CreateContext();

        await _context.Database.EnsureDeletedAsync();
        await _context.Database.MigrateAsync();

        _repository = new BookingRepository(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBooking_ReturnsBooking()
    {
        var eventId = await SeedEventAsync();
        var booking = CreateBooking(eventId);
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.EventId, result.EventId);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingBooking_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFilteredAsync_NullStatus_ReturnsAllBookings()
    {
        var eventId = await SeedEventAsync();
        _context.Bookings.AddRange(
            CreateBooking(eventId),
            CreateBooking(eventId, BookingStatus.Confirmed),
            CreateBooking(eventId, BookingStatus.Rejected));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new BookingQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_WithPendingStatus_ReturnsOnlyPending()
    {
        var eventId = await SeedEventAsync();
        _context.Bookings.AddRange(
            CreateBooking(eventId),
            CreateBooking(eventId),
            CreateBooking(eventId, BookingStatus.Confirmed),
            CreateBooking(eventId, BookingStatus.Rejected));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new BookingQuery(Status: BookingStatus.Pending),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }

    [Fact]
    public async Task GetFilteredAsync_WithConfirmedStatus_ReturnsOnlyConfirmed()
    {
        var eventId = await SeedEventAsync();
        _context.Bookings.AddRange(
            CreateBooking(eventId),
            CreateBooking(eventId, BookingStatus.Confirmed),
            CreateBooking(eventId, BookingStatus.Confirmed));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new BookingQuery(Status: BookingStatus.Confirmed),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(BookingStatus.Confirmed, b.Status));
    }

    [Fact]
    public async Task GetFilteredAsync_WithRejectedStatus_ReturnsOnlyRejected()
    {
        var eventId = await SeedEventAsync();
        _context.Bookings.AddRange(
            CreateBooking(eventId),
            CreateBooking(eventId, BookingStatus.Confirmed),
            CreateBooking(eventId, BookingStatus.Rejected));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new BookingQuery(Status: BookingStatus.Rejected),
            TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(BookingStatus.Rejected, result[0].Status);
    }

    [Fact]
    public async Task GetFilteredAsync_NoMatchingStatus_ReturnsEmpty()
    {
        var eventId = await SeedEventAsync();
        
        _context.Bookings.AddRange(
            CreateBooking(eventId, BookingStatus.Confirmed),
            CreateBooking(eventId, BookingStatus.Rejected));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new BookingQuery(Status: BookingStatus.Pending),
            TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFilteredAsync_EmptyTable_ReturnsEmpty()
    {
        var result = await _repository.GetFilteredAsync(new BookingQuery(), TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_ValidBooking_PersistsBooking()
    {
        var eventId = await SeedEventAsync();
        var booking = CreateBooking(eventId);

        await _repository.AddAsync(booking, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fromDb = await _context.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == booking.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(fromDb);
        Assert.Equal(booking.EventId, fromDb.EventId);
        Assert.Equal(BookingStatus.Pending, fromDb.Status);
    }

    [Fact]
    public async Task UpdateAsync_ExistingBooking_UpdatesStatus()
    {
        var eventId = await SeedEventAsync();
        var booking = CreateBooking(eventId);
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        booking.Status = BookingStatus.Confirmed;
        booking.ProcessedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(booking, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fromDb = await _context.Bookings.AsNoTracking()
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Confirmed, fromDb.Status);
        Assert.NotNull(fromDb.ProcessedAt);
    }

    [Fact]
    public async Task UpdateAsync_ExistingBooking_UpdatesMultipleFields()
    {
        var eventId = await SeedEventAsync();
        var booking = CreateBooking(eventId);
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        booking.Status = BookingStatus.Rejected;
        booking.ProcessedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(booking, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fromDb = await _context.Bookings.AsNoTracking()
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Rejected, fromDb.Status);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingBooking_ThrowsKeyNotFound()
    {
        var eventId = await SeedEventAsync();
        var booking = CreateBooking(eventId);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repository.UpdateAsync(booking, TestContext.Current.CancellationToken));
    }

    private static Booking CreateBooking(Guid eventId, BookingStatus status = BookingStatus.Pending)
    {
        return new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };
    }
    
    private async Task<Guid> SeedEventAsync()
    {
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 100,
            AvailableSeats = 100
        };

        _context.Events.Add(evt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return evt.Id;
    }
}