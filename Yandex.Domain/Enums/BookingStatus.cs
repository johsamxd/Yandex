namespace Yandex.Domain.Enums;

public enum BookingStatus
{
    /// <summary>
    /// Booking has been created and is waiting for processing
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Booking has been confirmed
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Booking has been rejected
    /// </summary>
    Rejected = 2
}