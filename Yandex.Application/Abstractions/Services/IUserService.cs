using Yandex.Application.Dtos.Users;
using Yandex.Application.Requests.Users;

namespace Yandex.Application.Abstractions.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}