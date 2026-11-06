using Yandex.Application.Dtos.Bookings;
using Yandex.Domain.Entities;

namespace Yandex.Application.Abstractions.Persistence.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetFilteredAsync(BookingQuery query, CancellationToken cancellationToken = default);

    Task AddAsync(Booking entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(Booking entity, CancellationToken cancellationToken = default);
}