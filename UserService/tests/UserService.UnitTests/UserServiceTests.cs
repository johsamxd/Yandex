using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Enums;
using Shared.Domain.Exceptions;
using UserService.Application.Abstractions.Persistence.Repositories;
using UserService.Application.Abstractions.Security;
using UserService.Application.Abstractions.Services;
using UserService.Application.Requests.Users;

namespace UserService.UnitTests;

public class UserServiceTests : IDisposable
{
    private readonly TestServiceProvider _provider = new();
    private readonly IServiceScope _scope;
    private readonly IUserService _service;
    private readonly IUserRepository _repository;

    public UserServiceTests()
    {
        _scope = _provider.CreateScope();
        _service = _scope.ServiceProvider.GetRequiredService<IUserService>();
        _repository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public async Task Register_WithoutRole_CreatesUserWithUserRole()
    {
        // Act
        var result = await _service.RegisterAsync(new RegisterRequest("alice", "password1", null),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("alice", result.Login);
        Assert.Equal(UserRole.User, result.Role);
        Assert.NotNull(await _repository.GetByLoginAsync("alice", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        // Act
        await _service.RegisterAsync(new RegisterRequest("bob", "password1", null),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var user = await _repository.GetByLoginAsync("bob", TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        Assert.NotEqual("password1", user.PasswordHash);
        Assert.True(_scope.ServiceProvider.GetRequiredService<IPasswordHasher>()
            .Verify("password1", user.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateLogin_ThrowsValidationException()
    {
        // Arrange
        await _service.RegisterAsync(new RegisterRequest("carol", "password1", null),
            cancellationToken: TestContext.Current.CancellationToken);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.RegisterAsync(new RegisterRequest("carol", "password2", null),
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Register_AdminRoleByAnonymous_ThrowsForbidden()
    {
        // Act + Assert — самостоятельная регистрация администратора запрещена
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.RegisterAsync(new RegisterRequest("mallory", "password1", UserRole.Admin),
                callerIsAdmin: false, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(await _repository.GetByLoginAsync("mallory", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Register_AdminRoleByAdmin_CreatesAdmin()
    {
        // Act
        var result = await _service.RegisterAsync(new RegisterRequest("dave", "password1", UserRole.Admin),
            callerIsAdmin: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(UserRole.Admin, result.Role);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        await _service.RegisterAsync(new RegisterRequest("erin", "password1", null),
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var result = await _service.LoginAsync(new LoginRequest("erin", "password1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("token-for-erin", result.Token);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsValidationException()
    {
        // Arrange
        await _service.RegisterAsync(new RegisterRequest("frank", "password1", null),
            cancellationToken: TestContext.Current.CancellationToken);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.LoginAsync(new LoginRequest("frank", "wrong-password"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Login_UnknownUser_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.LoginAsync(new LoginRequest("nobody", "password1"), TestContext.Current.CancellationToken));
    }
}
