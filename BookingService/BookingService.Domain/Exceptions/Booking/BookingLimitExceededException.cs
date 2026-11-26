namespace BookingService.Domain.Exceptions.Booking;

public class BookingLimitExceededException(string message) : Exception(message);