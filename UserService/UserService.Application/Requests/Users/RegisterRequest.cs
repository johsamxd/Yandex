using UserService.Domain.Enums;

namespace UserService.Application.Requests.Users;

public record RegisterRequest(string Login, string Password, UserRole? Role);