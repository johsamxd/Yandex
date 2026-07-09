using Microsoft.Extensions.DependencyInjection;
using Yandex.Domain.Abstractions;
using Yandex.Infrastructure.Persistence;

namespace Yandex.Infrastructure;

public static class InfrastructureServicesExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));

        return services;
    }
}