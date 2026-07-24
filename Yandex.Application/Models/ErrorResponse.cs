namespace Yandex.Application.Models;

public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance
);