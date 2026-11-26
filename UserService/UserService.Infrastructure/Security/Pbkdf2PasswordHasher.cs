using System.Security.Cryptography;
using UserService.Application.Abstractions.Security;

namespace UserService.Infrastructure.Security;

/// <summary>
/// Хеширование пароля алгоритмом PBKDF2-HMAC-SHA256 со случайной солью (встроено в .NET, без сторонних пакетов).
/// Формат хеша: <c>v1.{итерации}.{соль base64}.{хеш base64}</c> — параметры хранятся вместе с хешем,
/// поэтому число итераций можно повышать без потери совместимости со старыми записями.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Version = "v1";
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return $"{Version}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 4 || parts[0] != Version || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            // Неизвестный формат (например, старый SHA-256 без соли) — пароль не подходит
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
