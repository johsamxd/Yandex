using Shared.Domain.Enums;

namespace UserService.Application.Dtos.Users;

public record UserDto(Guid Id, string Login, UserRole Role);