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

    [Required]
    public required int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public Event()
    {
        AvailableSeats = TotalSeats;
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats - count < 0)
        {
            return false;
        }

        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;
    }
}