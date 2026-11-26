using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions.Persistence;
using Shared.Domain.Enums;
using UserService.Application.Abstractions.Persistence.Repositories;
using UserService.Application.Abstractions.Security;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence;

/// <summary>
/// Создаёт первого администратора при старте сервиса (секция конфигурации <c>AdminSeed</c>).
/// Самостоятельная регистрация с ролью Admin запрещена, поэтому без сида администратора не будет.
/// Повторный запуск безопасен: существующий пользователь не изменяется.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, string? login, string? password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var repository = services.GetRequiredService<IUserRepository>();
        if (await repository.GetByLoginAsync(login, cancellationToken) is not null)
        {
            return;
        }

        var hasher = services.GetRequiredService<IPasswordHasher>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        await repository.AddAsync(new User
        {
            Login = login,
            PasswordHash = hasher.Hash(password),
            Role = UserRole.Admin
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
