using EventService.Domain.Entities;

namespace EventService.Application.Abstractions.Persistence.Repositories;

public interface IProcessedBookingRepository
{
    Task<bool> ExistsAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task AddAsync(ProcessedBooking entity, CancellationToken cancellationToken = default);
}
