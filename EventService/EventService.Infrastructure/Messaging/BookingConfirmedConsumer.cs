using System.Text.Json;
using Confluent.Kafka;
using EventService.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Application.Options;
using Shared.Contracts.Events;
using Shared.Contracts.Topics;

namespace EventService.Infrastructure.Messaging;

/// <summary>
/// Фоновый подписчик на топик <see cref="KafkaTopics.BookingConfirmed"/>.
/// <para>
/// BackgroundService — singleton, а обработчик и DbContext — scoped, поэтому на каждое сообщение
/// создаётся отдельный scope. Метод <c>Consume</c> блокирующий, поэтому цикл вынесен в Task.Run,
/// чтобы не задерживать запуск остальных hosted-сервисов.
/// </para>
/// </summary>
public sealed class BookingConfirmedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<BookingConfirmedConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => RunAsync(stoppingToken), stoppingToken);

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        var kafka = options.Value;

        var config = new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = kafka.ConsumerGroup,
            // Новая группа читает топик с начала: брони, подтверждённые до первого запуска, не потеряются
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // Offset фиксируем вручную — только после успешной обработки сообщения
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => logger.LogWarning("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        consumer.Subscribe(KafkaTopics.BookingConfirmed);
        logger.LogInformation(
            "Subscribed to {Topic} as group {Group} ({Servers})",
            KafkaTopics.BookingConfirmed, kafka.ConsumerGroup, kafka.BootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    // Например, топик ещё не создан — подождём и попробуем снова
                    logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    await Task.Delay(RetryDelay, stoppingToken);
                    continue;
                }

                if (result?.Message is null)
                {
                    continue;
                }

                await ProcessAsync(consumer, result, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальная остановка приложения
        }
        finally
        {
            // Покидаем группу корректно, чтобы раздел быстро перешёл другому экземпляру
            consumer.Close();
            logger.LogInformation("Consumer of {Topic} stopped", KafkaTopics.BookingConfirmed);
        }
    }

    private async Task ProcessAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken stoppingToken)
    {
        BookingConfirmed? message;
        try
        {
            message = string.IsNullOrWhiteSpace(result.Message.Value)
                ? null
                : JsonSerializer.Deserialize<BookingConfirmed>(result.Message.Value, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Skipping malformed message at {Position}", result.TopicPartitionOffset);
            consumer.Commit(result);
            return;
        }

        if (message is null)
        {
            logger.LogError("Skipping empty message at {Position}", result.TopicPartitionOffset);
            consumer.Commit(result);
            return;
        }

        try
        {
            // Отдельный scope на каждое сообщение: репозитории и DbContext — scoped
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IBookingConfirmedHandler>();

            await handler.HandleAsync(message, stoppingToken);

            consumer.Commit(result);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Непредвиденная (обычно временная) ошибка, например БД недоступна:
            // возвращаемся на это же сообщение и повторяем позже. Повтор безопасен — обработчик идемпотентен.
            logger.LogError(ex,
                "Failed to process BookingConfirmed for booking {BookingId} at {Position}, will retry",
                message.BookingId, result.TopicPartitionOffset);

            consumer.Seek(result.TopicPartitionOffset);
            await Task.Delay(RetryDelay, stoppingToken);
        }
    }
}
