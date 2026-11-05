using Microsoft.EntityFrameworkCore;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;

namespace Yandex.Infrastructure.Persistence.Repositories;

public class EventRepository(AppDbContext context) : IEventRepository
{
    private readonly DbSet<Event> _set = context.Set<Event>();

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _set.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Event>> GetFilteredAsync(
        EventQuery filter, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(filter)
            .OrderBy(e => e.StartAt)
            .ThenBy(e => e.Id) 
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(EventQuery filter, CancellationToken cancellationToken = default) =>
        await BuildQuery(filter).CountAsync(cancellationToken);

    public async Task AddAsync(Event entity, CancellationToken cancellationToken = default) =>
        await _set.AddAsync(entity, cancellationToken);

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _set.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Entity with id {id} not found");
        }

        _set.Remove(existing);
    }

    private IQueryable<Event> BuildQuery(EventQuery filter)
    {
        var query = _set.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var title = filter.Title.Trim();
            query = query.Where(e => EF.Functions.ILike(e.Title, $"%{title}%"));
        }

        if (filter.From.HasValue)
        {
            var startDate = DateTime.SpecifyKind(filter.From.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.StartAt >= startDate);
        }

        if (filter.To.HasValue)
        {
            var endDate = DateTime.SpecifyKind(filter.To.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(e => e.EndAt <= endDate);
        }

        return query;
    }
}