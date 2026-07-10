using System.ComponentModel.DataAnnotations;

namespace Yandex.Domain.Entities;

public class Event : BaseEntity
{
    [Required]
    public required string Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public required DateTime StartAt { get; set; }

    [Required]
    public required DateTime EndAt { get; set; }
}