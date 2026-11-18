using UserService.Domain.Entities;

namespace UserService.Application.Abstractions.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}