using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Enums;
using UserService.Application.Abstractions.Persistence.Repositories;
using UserService.Application.Abstractions.Security;
using UserService.Infrastructure.Persistence;

namespace UserService.UnitTests;

public class AdminSeederTests
{
    [Fact]
    public async Task Seed_CreatesAdminWithHashedPassword()
    {
        using var provider = new TestServiceProvider();
        using var scope = provider.CreateScope();

        await AdminSeeder.SeedAsync(scope.ServiceProvider, "root", "root-pass", TestContext.Current.CancellationToken);

        var user = await scope.ServiceProvider.GetRequiredService<IUserRepository>()
            .GetByLoginAsync("root", TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.True(scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Verify("root-pass", user.PasswordHash));
    }

    [Fact]
    public async Task Seed_CalledTwice_DoesNotChangeExistingUser()
    {
        using var provider = new TestServiceProvider();
        using var scope = provider.CreateScope();
        await AdminSeeder.SeedAsync(scope.ServiceProvider, "root", "root-pass", TestContext.Current.CancellationToken);
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var hashBefore = (await repository.GetByLoginAsync("root", TestContext.Current.CancellationToken))!.PasswordHash;

        await AdminSeeder.SeedAsync(scope.ServiceProvider, "root", "another-pass", TestContext.Current.CancellationToken);

        var after = await repository.GetByLoginAsync("root", TestContext.Current.CancellationToken);
        Assert.Equal(hashBefore, after!.PasswordHash);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "pass")]
    [InlineData("root", " ")]
    public async Task Seed_WithoutCredentials_DoesNothing(string? login, string? password)
    {
        using var provider = new TestServiceProvider();
        using var scope = provider.CreateScope();

        await AdminSeeder.SeedAsync(scope.ServiceProvider, login, password, TestContext.Current.CancellationToken);

        Assert.Null(await scope.ServiceProvider.GetRequiredService<IUserRepository>()
            .GetByLoginAsync("root", TestContext.Current.CancellationToken));
    }
}
