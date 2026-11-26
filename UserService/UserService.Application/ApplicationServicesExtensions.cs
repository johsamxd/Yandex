using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Abstractions.Services;

namespace UserService.Application;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServicesExtensions).Assembly);

        // Custom services
        services.AddScoped<IUserService, Services.UserService>();

        return services;
    }
}