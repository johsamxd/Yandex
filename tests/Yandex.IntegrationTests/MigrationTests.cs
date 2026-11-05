using Microsoft.EntityFrameworkCore;
using Npgsql;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;
using Yandex.Infrastructure.Persistence;

namespace Yandex.IntegrationTests;

[Collection("Postgres collection")]
public class MigrationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private AppDbContext _context = null!;

    public async ValueTask InitializeAsync()
    {
        _context = fixture.CreateContext();

        await _context.Database.EnsureDeletedAsync();
        await _context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Migrations_ApplyCleanly_NoPendingMigrations()
    {
        // Arrange
        // (миграции уже применены в InitializeAsync)

        // Act
        var applied = await _context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        var pending = await _context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(applied, m => m.Contains("Initial", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(pending);
    }

    [Fact]
    public async Task Schema_ContainsRequiredTables()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(_context.Database.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = new NpgsqlCommand(
            @"SELECT table_name 
              FROM information_schema.tables 
              WHERE table_schema = 'public' 
                AND table_name IN ('Events', 'Bookings')",
            connection);

        // Act
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var tables = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        // Assert
        Assert.Contains("Events", tables);
        Assert.Contains("Bookings", tables);
    }

    [Fact]
    public async Task Schema_HasForeignKeyFromBookingsToEvents()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(_context.Database.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = new NpgsqlCommand(
            @"SELECT constraint_name 
              FROM information_schema.table_constraints 
              WHERE table_schema = 'public'
                AND table_name = 'Bookings'
                AND constraint_type = 'FOREIGN KEY'",
            connection);

        // Act
        var constraintNames = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            constraintNames.Add(reader.GetString(0));
        }

        // Assert
        Assert.Contains(constraintNames, name =>
            name.Contains("FK_Bookings_Events", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CascadeDelete_RemovesBookingsWhenEventDeleted()
    {
        // Arrange: создаём событие и бронь
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Cascade Test Event",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 10,
            AvailableSeats = 10
        };
        _context.Events.Add(evt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: удаляем событие (каскад должен удалить бронь)
        _context.Events.Remove(evt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert: брони больше нет
        var bookingExists = await _context.Bookings
            .AsNoTracking()
            .AnyAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);

        Assert.False(bookingExists);
    }
}