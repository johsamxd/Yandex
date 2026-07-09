using System.ComponentModel.DataAnnotations;

namespace Yandex.Application.Dtos.Events;

public record UpdateEventRequest(
    [Required(ErrorMessage = "Id не должен быть пустым")]
    Guid Id,
    [Required(ErrorMessage = "Title не должен быть пустым")]
    string Title,

    string Description,

    [Required(ErrorMessage = "StartAt не должен быть пустым")]
    DateTime StartAt,

    [Required(ErrorMessage = "EndAt не должен быть пустым")]
    DateTime EndAt
);