using BookingService.Application.Abstractions.Messaging;
using BookingService.Application.Abstractions.Persistence.Repositories;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Persistence;
using BookingService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions.Persistence;

namespace BookingService.Infrastructure;

public static class InfrastructureServicesExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        
        services.AddSingleton<IBookingEventPublisher, KafkaBookingEventPublisher>();

        // Kafka-продюсер — singleton: создаётся один раз и освобождается (IDisposable) при остановке приложения
        services.AddSingleton<IBookingEventPublisher, KafkaBookingEventPublisher>();

        return services;
    }
}