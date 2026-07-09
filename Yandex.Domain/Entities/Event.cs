namespace Yandex.Domain.Entities;

public class Event : BaseEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTime StartAt { get; set; }
    public required DateTime EndAt { get; set; }
}