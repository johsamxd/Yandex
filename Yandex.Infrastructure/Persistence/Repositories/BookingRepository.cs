using Microsoft.EntityFrameworkCore;
using Yandex.Application.Abstractions.Persistence.Repositories;
using Yandex.Application.Dtos.Bookings;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;

namespace Yandex.Infrastructure.Persistence.Repositories;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    private readonly DbSet<Booking> _set = context.Set<Booking>();

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _set.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetFilteredAsync(BookingQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Status == null)
        {
            return await _set.AsNoTracking().ToListAsync(cancellationToken);
        }

        return await _set
            .Where(b => b.Status == query.Status)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Booking entity, CancellationToken cancellationToken = default) =>
        await _set.AddAsync(entity, cancellationToken);

    public async Task UpdateAsync(Booking entity, CancellationToken cancellationToken = default)
    {
        var existing = await _set.FindAsync([entity.Id], cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Entity with id {entity.Id} not found");
        }

        context.Entry(existing).CurrentValues.SetValues(entity);
    }

    public Task<int> CountActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _set.CountAsync(b => b.UserId == userId &&
                             (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed),
            cancellationToken);
}