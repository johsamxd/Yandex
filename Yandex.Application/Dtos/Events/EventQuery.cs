namespace Yandex.Application.Dtos.Events;

public record EventQuery(
    string? Title = null,
    DateTime? From = null,
    DateTime? To = null
);