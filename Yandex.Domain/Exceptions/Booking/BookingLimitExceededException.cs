namespace Yandex.Domain.Exceptions.Booking;

public class BookingLimitExceededException(string message) : Exception(message);