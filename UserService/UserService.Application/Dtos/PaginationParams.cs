namespace UserService.Application.Dtos;

public record PaginationParams(int Page = 1, int PageSize = 8);