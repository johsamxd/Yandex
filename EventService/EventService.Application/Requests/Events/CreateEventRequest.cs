namespace EventService.Application.Requests.Events;

public record CreateEventRequest(
    string Title,
    string Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats
);