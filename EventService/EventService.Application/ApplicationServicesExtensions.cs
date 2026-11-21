using EventService.Application.Abstractions.Services;
using EventService.Application.Automapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Application;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<EventProfile>();
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationServicesExtensions).Assembly);

        // Custom services
        services.AddScoped<IEventService, Services.EventService>();

        return services;
    }
}