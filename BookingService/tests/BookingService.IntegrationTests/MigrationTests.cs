using BookingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BookingService.IntegrationTests;

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
}