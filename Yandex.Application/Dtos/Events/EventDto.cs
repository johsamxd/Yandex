namespace Yandex.Application.Dtos.Events;

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartAt,
    DateTime EndAt
);