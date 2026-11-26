namespace Shared.Contracts.Topics;

/// <summary>
/// Имена топиков Kafka. Издатель и подписчик ссылаются на одну и ту же константу,
/// поэтому строка с именем топика не дублируется в сервисах.
/// </summary>
public static class KafkaTopics
{
    /// <summary>
    /// Топик с событиями <see cref="Events.BookingConfirmed"/>.
    /// Публикует Bookings, читает Events.
    /// </summary>
    public const string BookingConfirmed = "booking-confirmed";
}
