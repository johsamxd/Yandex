using System.Text.Json;
using BookingService.Application.Abstractions.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Application.Options;
using Shared.Contracts.Events;
using Shared.Contracts.Topics;

namespace BookingService.Infrastructure.Messaging;

/// <summary>
/// Публикатор событий Bookings в Kafka.
/// Продюсер — тяжёлый потокобезопасный объект: создаётся один раз (класс регистрируется как singleton)
/// и освобождается при остановке приложения.
/// </summary>
public sealed class KafkaBookingEventPublisher : IBookingEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaBookingEventPublisher> _logger;

    public KafkaBookingEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaBookingEventPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            // Подтверждение от всех реплик + идемпотентный продюсер: без потерь и дублей при ретраях клиента
            Acks = Acks.All,
            EnableIdempotence = true,
            // Не ждём брокера дефолтные 5 минут, если Kafka недоступна
            MessageTimeoutMs = 15_000
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                if (error.IsFatal)
                    _logger.LogError("Kafka producer fatal error: {Reason}", error.Reason);
                else
                    _logger.LogWarning("Kafka producer error: {Reason}", error.Reason);
            })
            .Build();
    }

    public async Task PublishBookingConfirmedAsync(BookingConfirmed message,
        CancellationToken cancellationToken = default)
    {
        var kafkaMessage = new Message<string, string>
        {
            // Ключ = EventId: все брони одного события попадают в один раздел и обрабатываются по порядку
            Key = message.EventId.ToString(),
            Value = JsonSerializer.Serialize(message, JsonOptions)
        };

        var result = await _producer.ProduceAsync(KafkaTopics.BookingConfirmed, kafkaMessage, cancellationToken);

        _logger.LogInformation(
            "BookingConfirmed for booking {BookingId} published to {Topic} [partition {Partition}, offset {Offset}]",
            message.BookingId, result.Topic, result.Partition.Value, result.Offset.Value);
    }

    public void Dispose()
    {
        try
        {
            // Дожидаемся отправки сообщений, оставшихся в буфере
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        finally
        {
            _producer.Dispose();
        }
    }
}
