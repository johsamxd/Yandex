using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Yandex.Application.Abstractions.Services;
using Yandex.Application.Automapper;
using Yandex.Application.Services;

namespace Yandex.Application;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<EventProfile>();
            cfg.AddProfile<BookingProfile>();
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationServicesExtensions).Assembly);

        // Custom services
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        // Background services
        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}