using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Yandex.Application.Abstractions;
using Yandex.Application.Automapper;
using Yandex.Application.Services;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Yandex.Application.FluentValidation;

namespace Yandex.Application;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<EventProfile>();
        });

        services.AddFluentValidationAutoValidation(configuration =>
        {
            configuration.OverrideDefaultResultFactoryWith<FluentValidationResponseFactory>();
        });
        services.AddValidatorsFromAssembly(typeof(ApplicationServicesExtensions).Assembly);

        services.AddScoped<IEventService, EventService>();

        return services;
    }
}