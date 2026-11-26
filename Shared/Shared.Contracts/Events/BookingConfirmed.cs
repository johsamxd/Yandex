namespace Shared.Contracts.Events;

/// <summary>
/// Контракт события «бронь подтверждена». Публичный «договор» между сервисом Bookings (издатель)
/// и сервисом Events (подписчик). Содержит только то, что нужно подписчику.
/// </summary>
/// <param name="BookingId">Идентификатор брони. Подписчик использует его для защиты от повторной обработки.</param>
/// <param name="EventId">Идентификатор события, у которого нужно уменьшить количество свободных мест.</param>
/// <param name="UserId">Идентификатор пользователя, создавшего бронь.</param>
/// <param name="Seats">Сколько мест занимает бронь.</param>
/// <param name="ConfirmedAt">Момент подтверждения брони (UTC).</param>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int Seats,
    DateTime ConfirmedAt);
