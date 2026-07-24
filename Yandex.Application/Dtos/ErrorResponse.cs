namespace Yandex.Application.Dtos;

public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance
);