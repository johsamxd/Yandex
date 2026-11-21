using EventService.Application;
using EventService.Application.Abstractions.Persistence.Repositories;
using EventService.Infrastructure.Persistence;
using EventService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions.Persistence;

namespace EventService.UnitTests;

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
        services.AddScoped<IEventRepository, EventRepository>();

        // Application
        services.AddApplicationServices();

        _provider = services.BuildServiceProvider();
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    public void Dispose() => _provider.Dispose();
}