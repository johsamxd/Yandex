using AutoMapper;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos;
using Yandex.Application.Dtos.Events;
using Yandex.Application.Exceptions;
using Yandex.Application.Requests.Events;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;

namespace Yandex.Application.Services;

public class EventService(IEntityRepository<Event> repository, IUnitOfWork unitOfWork, IMapper mapper) : IEventService
{
    public async Task<PaginatedResult<EventDto>> GetEventsAsync(EventFilter filter, CancellationToken cancellationToken = default)
    {
        var query = repository.Query();

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var title = filter.Title.Trim();
            query = query.Where(e => e.Title.Contains(title, StringComparison.CurrentCultureIgnoreCase));
        }

        if (filter.From.HasValue)
        {
            var startDate = filter.From.Value.Date;
            query = query.Where(e => e.StartAt >= startDate);
        }

        if (filter.To.HasValue)
        {
            var endDate = filter.To.Value.Date;
            query = query.Where(e => e.EndAt <= endDate);
        }

        var filtered = await repository.ToListAsync(query, cancellationToken);

        var data = filtered
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var items = mapper.Map<IEnumerable<EventDto>>(data);
        var totalCount = filtered.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

        return new PaginatedResult<EventDto>(items, filter.Page, data.Count, totalPages, totalCount);
    }

    public async Task<EventDto> GetEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await repository.GetByIdAsync(id, cancellationToken);
        if (data == null)
        {
            throw new NotFoundException($"Event with id {id} not found");
        }

        return mapper.Map<EventDto>(data);
    }

    public async Task<EventDto> CreateEventAsync(CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        var data = mapper.Map<Event>(request);
        data.AvailableSeats = request.TotalSeats;
        await repository.AddAsync(data, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<EventDto>(data);
    }

    public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        var data = await repository.GetByIdAsync(id, cancellationToken);
        if (data == null)
        {
            throw new NotFoundException($"Event with id {id} not found");
        }

        mapper.Map(request, data);
        await repository.UpdateAsync(data, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<EventDto>(data);
    }

    public async Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await repository.GetByIdAsync(id, cancellationToken);
        if (data == null)
        {
            throw new NotFoundException($"Event with id {id} not found");
        }

        await repository.RemoveAsync(id,  cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}