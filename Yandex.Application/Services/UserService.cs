using Yandex.Application.Abstractions.Persistence;
using Yandex.Application.Abstractions.Persistence.Repositories;
using Yandex.Application.Abstractions.Security;
using Yandex.Application.Abstractions.Services;
using Yandex.Application.Dtos.Users;
using Yandex.Application.Requests.Users;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;
using Yandex.Domain.Exceptions;

namespace Yandex.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IUserService
{
    public async Task<UserDto> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await userRepository.GetByLoginAsync(request.Login, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"User with login: {request.Login} already exists");

        var role = request.Role ?? UserRole.User;
        var passwordHash = passwordHasher.Hash(request.Password);
        var user = new User
        {
            Login = request.Login,
            PasswordHash = passwordHash,
            Role = role,
        };

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserDto(user.Id, user.Login, user.Role);
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByLoginAsync(request.Login, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid login or password");

        var token = jwtTokenGenerator.GenerateToken(user);
        return new LoginResponse(token);
    }
}