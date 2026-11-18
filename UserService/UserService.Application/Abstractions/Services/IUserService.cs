using UserService.Application.Dtos.Users;
using UserService.Application.Requests.Users;

namespace UserService.Application.Abstractions.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}