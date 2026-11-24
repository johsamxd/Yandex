using BookingService.Application.Abstractions.Services;
using BookingService.Application.Automapper;
using BookingService.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Application;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<BookingProfile>();
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationServicesExtensions).Assembly);

        // Custom services
        services.AddScoped<IBookingService, Services.BookingService>();

        // Background services
        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}