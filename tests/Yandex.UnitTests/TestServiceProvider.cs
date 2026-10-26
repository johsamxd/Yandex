using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yandex.Application;
using Yandex.Domain.Abstractions;
using Yandex.Infrastructure.Persistence;

namespace Yandex.UnitTests;

internal sealed class TestServiceProvider : IDisposable
{
    private readonly ServiceProvider _provider;

    public TestServiceProvider()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        // Infrastructure
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));

        // Application
        services.AddApplicationServices();

        _provider = services.BuildServiceProvider();
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    public void Dispose() => _provider.Dispose();
}