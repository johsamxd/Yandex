using Yandex.Domain.Enums;

namespace Yandex.Application.Dtos.Users;

public record UserDto(Guid Id, string Login, UserRole Role);