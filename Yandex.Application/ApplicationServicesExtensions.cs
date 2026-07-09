using Microsoft.Extensions.DependencyInjection;
using Yandex.Application.Abstractions;
using Yandex.Application.Automapper;
using Yandex.Application.Services;

namespace Yandex.Application;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.LicenseKey = "<License Key Here>";
            cfg.AddProfile<EventProfile>();
        });

        services.AddScoped<IEventService, EventService>();

        return services;
    }
}