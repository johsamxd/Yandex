using System.ComponentModel.DataAnnotations;

namespace Yandex.Application.Dtos;

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartAt,
    DateTime EndAt
);