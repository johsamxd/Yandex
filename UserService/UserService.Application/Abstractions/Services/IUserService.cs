using UserService.Application.Dtos.Users;
using UserService.Application.Requests.Users;

namespace UserService.Application.Abstractions.Services;

public interface IUserService
{
    /// <param name="request">Данные регистрации.</param>
    /// <param name="callerIsAdmin">Запрос выполняет авторизованный администратор — только он может создавать администраторов.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<UserDto> RegisterAsync(RegisterRequest request, bool callerIsAdmin = false,
        CancellationToken cancellationToken = default);

    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}