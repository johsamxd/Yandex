using EventService.Application.Abstractions.Persistence.Repositories;
using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence.Repositories;

public class ProcessedBookingRepository(AppDbContext context) : IProcessedBookingRepository
{
    private readonly DbSet<ProcessedBooking> _set = context.Set<ProcessedBooking>();

    public async Task<bool> ExistsAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        await _set.AnyAsync(x => x.BookingId == bookingId, cancellationToken);

    public async Task AddAsync(ProcessedBooking entity, CancellationToken cancellationToken = default) =>
        await _set.AddAsync(entity, cancellationToken);
}
