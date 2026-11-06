namespace Yandex.Domain.Exceptions.Booking;

public class NoAvailableSeatsException(string message) : Exception(message);