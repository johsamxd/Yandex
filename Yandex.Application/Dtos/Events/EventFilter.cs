namespace Yandex.Application.Dtos.Events;

public record EventFilter(
    string? Title = null,
    DateTime? StartAt = null,
    DateTime? EndAt = null,
    int Page = 1,
    int PageSize = 8
) : PaginationParams(Page, PageSize);