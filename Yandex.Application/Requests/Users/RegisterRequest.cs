using Yandex.Domain.Enums;

namespace Yandex.Application.Requests.Users;

public record RegisterRequest(string Login, string Password, UserRole? Role);