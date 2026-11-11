using AutoMapper;
using Yandex.Application.Abstractions.Persistence;
using Yandex.Application.Abstractions.Persistence.Repositories;
using Yandex.Application.Abstractions.Services;
using Yandex.Application.Dtos;
using Yandex.Application.Dtos.Events;
using Yandex.Application.Requests.Events;
using Yandex.Domain.Entities;
using Yandex.Domain.Exceptions;

namespace Yandex.Application.Services;

public class EventService(IEventRepository repository, IUnitOfWork unitOfWork, IMapper mapper) : IEventService
{
    public async Task<PaginatedResult<EventDto>> GetEventsAsync(
        EventFilter filter, CancellationToken cancellationToken = default)
    {
        var query = new EventQuery(filter.Title, filter.From, filter.To);

        var totalCount = await repository.CountAsync(query, cancellationToken);
        var data = await repository.GetFilteredAsync(query, filter.Page, filter.PageSize, cancellationToken);

        var items = mapper.Map<IEnumerable<EventDto>>(data);
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

    public async Task<EventDto> CreateEventAsync(CreateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var data = mapper.Map<Event>(request);
        data.AvailableSeats = request.TotalSeats;
        await repository.AddAsync(data, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<EventDto>(data);
    }

    public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var data = await repository.GetByIdAsync(id, cancellationToken);
        if (data == null)
        {
            throw new NotFoundException($"Event with id {id} not found");
        }

        mapper.Map(request, data);
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

        await repository.RemoveAsync(id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}