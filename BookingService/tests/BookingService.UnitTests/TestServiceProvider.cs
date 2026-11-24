using BookingService.Application;
using BookingService.Application.Abstractions.Persistence.Repositories;
using BookingService.Infrastructure.Persistence;
using BookingService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions.Persistence;

namespace BookingService.UnitTests;

internal sealed class TestServiceProvider : IDisposable
{
    private readonly ServiceProvider _provider;

    public TestServiceProvider()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddLogging();

        // Infrastructure
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        // Application
        services.AddApplicationServices();

        _provider = services.BuildServiceProvider();
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    public void Dispose() => _provider.Dispose();
}