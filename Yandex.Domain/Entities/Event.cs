using System.ComponentModel.DataAnnotations;

namespace Yandex.Domain.Entities;

public class Event : BaseEntity
{
    public required string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public required DateTime StartAt { get; set; }
    
    public required DateTime EndAt { get; set; }
    
    public required int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }
    
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    
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