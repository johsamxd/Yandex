using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace UserService.Domain.Entities;

public class User : BaseEntity
{
    public required string Login { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }
}