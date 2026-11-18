namespace UserService.Domain.Exceptions.Event;

public class EventStartedException(string message) : Exception(message);