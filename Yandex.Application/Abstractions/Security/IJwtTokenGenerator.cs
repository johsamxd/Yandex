using Yandex.Domain.Entities;

namespace Yandex.Application.Abstractions.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}