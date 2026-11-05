using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Yandex.Infrastructure.Persistence;

namespace Yandex.IntegrationTests;

[CollectionDefinition("Postgres collection")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("yandex_test")
        .Build();

    private string ConnectionString => _postgres.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}