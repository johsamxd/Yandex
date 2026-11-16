using Microsoft.EntityFrameworkCore;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;
using Yandex.Infrastructure.Persistence;
using Yandex.Infrastructure.Persistence.Repositories;

namespace Yandex.IntegrationTests;

[Collection("Postgres collection")]
public class UserRepositoryTests(PostgresFixture fixture) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private UserRepository _repository = null!;

    public async ValueTask InitializeAsync()
    {
        _context = fixture.CreateContext();
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.MigrateAsync();
        _repository = new UserRepository(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AddAsync_DuplicateLogin_ShouldThrowDbUpdateException()
    {
        // Arrange
        var user1 = new User
        {
            Login = "testuser",
            PasswordHash = "hash",
            Role = UserRole.Admin
        };
        await _repository.AddAsync(user1, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var user2 = new User
        {
            Login = "testuser",
            PasswordHash = "hash2",
            Role = UserRole.User
        };

        // Act & Assert: уникальный индекс на Login не даёт вставить дубликат
        await _repository.AddAsync(user2, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByLoginAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = "hash",
            Role = UserRole.Admin
        };
        await _repository.AddAsync(user, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByLoginAsync("testuser", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(UserRole.Admin, result.Role);
    }

    [Fact]
    public async Task GetByLoginAsync_NonExisting_ReturnsNull()
    {
        var result = await _repository.GetByLoginAsync("ghost", TestContext.Current.CancellationToken);
        Assert.Null(result);
    }
}