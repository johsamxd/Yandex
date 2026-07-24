namespace Yandex.Application.Requests.Events;

public record UpdateEventRequest(
    string Title,
    string Description,
    DateTime StartAt,
    DateTime EndAt
);