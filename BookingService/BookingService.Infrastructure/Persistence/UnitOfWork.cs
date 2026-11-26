using Shared.Application.Abstractions.Persistence;

namespace BookingService.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}