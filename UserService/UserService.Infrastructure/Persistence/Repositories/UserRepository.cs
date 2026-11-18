using Microsoft.EntityFrameworkCore;
using UserService.Application.Abstractions.Persistence.Repositories;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    private readonly DbSet<User> _set = context.Set<User>();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _set.FindAsync([id], cancellationToken).AsTask();

    public Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking().FirstOrDefaultAsync(u => u.Login == login, cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        _set.AddAsync(user, cancellationToken).AsTask();
}