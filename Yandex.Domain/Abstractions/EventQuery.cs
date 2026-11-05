namespace Yandex.Domain.Abstractions;

public record EventQuery(
    string? Title = null,
    DateTime? From = null,
    DateTime? To = null
);