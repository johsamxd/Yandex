namespace EventService.Domain.Entities;

/// <summary>
/// Запись о брони, событие <c>BookingConfirmed</c> по которой уже обработано (паттерн Inbox).
/// Нужна для идемпотентности: Kafka гарантирует доставку «минимум один раз», поэтому
/// одно и то же сообщение может прийти повторно, а места должны уменьшиться только однажды.
/// </summary>
public class ProcessedBooking
{
    /// <summary>Идентификатор брони из другого сервиса (ключ — связь только по идентификатору).</summary>
    public required Guid BookingId { get; init; }

    public required Guid EventId { get; init; }

    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
