using System.ComponentModel.DataAnnotations;

namespace Yandex.Application.Dtos.Events;

public record CreateEventRequest(
    [Required(ErrorMessage = "Title не должен быть пустым")]
    string Title,
    
    string Description,
    
    [Required(ErrorMessage = "StartAt не должен быть пустым")]
    DateTime StartAt,
    
    [Required(ErrorMessage = "EndAt не должен быть пустым")]
    DateTime EndAt
);