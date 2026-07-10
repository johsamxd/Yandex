using System.ComponentModel.DataAnnotations;

namespace Yandex.Application.Requests.Events;

public record UpdateEventRequest(
    Guid Id,
    string Title,
    string Description,
    DateTime StartAt,
    DateTime EndAt
);