using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Application.Options;
using Shared.Contracts.Topics;

namespace EventService.Infrastructure.Messaging;

/// <summary>
/// Создаёт топик <see cref="KafkaTopics.BookingConfirmed"/> при старте, если его ещё нет.
/// Регистрируется раньше подписчика, поэтому отрабатывает до его запуска — система работает
/// «из коробки» даже на пустом брокере. Ошибки не валят запуск сервиса.
/// </summary>
public sealed class KafkaTopicInitializer(
    IOptions<KafkaOptions> options,
    ILogger<KafkaTopicInitializer> logger) : IHostedService
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var kafka = options.Value;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var admin = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = kafka.BootstrapServers
                }).Build();

                await admin.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = KafkaTopics.BookingConfirmed,
                        NumPartitions = kafka.TopicPartitions,
                        ReplicationFactor = kafka.TopicReplicationFactor
                    }
                ], new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(10) });

                logger.LogInformation("Kafka topic {Topic} created", KafkaTopics.BookingConfirmed);
                return;
            }
            catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                logger.LogInformation("Kafka topic {Topic} already exists", KafkaTopics.BookingConfirmed);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Could not create Kafka topic {Topic} (attempt {Attempt}/{Max})",
                    KafkaTopics.BookingConfirmed, attempt, MaxAttempts);

                if (attempt < MaxAttempts)
                {
                    try
                    {
                        await Task.Delay(RetryDelay, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        // Сервис продолжает работу: топик может быть создан позже брокером или другим сервисом
        logger.LogWarning(
            "Kafka topic {Topic} was not created, continuing startup", KafkaTopics.BookingConfirmed);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
