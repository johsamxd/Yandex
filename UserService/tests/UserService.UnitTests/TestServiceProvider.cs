using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions.Persistence;
using UserService.Application;
using UserService.Application.Abstractions.Persistence.Repositories;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Persistence.Repositories;

namespace UserService.UnitTests;

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
        services.AddScoped<IUserRepository, UserRepository>();

        // Application
        services.AddApplicationServices();

        _provider = services.BuildServiceProvider();
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    public void Dispose() => _provider.Dispose();
}