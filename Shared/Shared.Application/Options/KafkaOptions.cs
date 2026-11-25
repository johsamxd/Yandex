namespace Shared.Application.Options;

/// <summary>
/// Настройки подключения к Apache Kafka. Секция конфигурации — <c>Kafka</c>.
/// В Docker переопределяются переменными окружения: <c>Kafka__BootstrapServers</c>, <c>Kafka__ConsumerGroup</c>.
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>Адрес брокера (host:port). Локально — localhost:9092, в Docker — kafka:29092.</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Группа потребителей. Нужна только сервисам-подписчикам (Events).
    /// Внутри одной группы каждое сообщение получает только один экземпляр сервиса.
    /// </summary>
    public string ConsumerGroup { get; set; } = string.Empty;

    /// <summary>Количество разделов при создании топика (используется сервисом, который создаёт топик).</summary>
    public int TopicPartitions { get; set; } = 3;

    /// <summary>Фактор репликации при создании топика.</summary>
    public short TopicReplicationFactor { get; set; } = 1;
}
