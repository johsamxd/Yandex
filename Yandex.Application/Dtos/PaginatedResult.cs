namespace Yandex.Application.Dtos;

public record PaginatedResult<T>(IEnumerable<T> Items, int Page, int Count, int TotalPages, int TotalItems);