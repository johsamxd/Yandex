namespace Yandex.Application.Exceptions.Booking;

public class NoAvailableSeatsException(string message) : Exception(message);