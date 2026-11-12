using Yandex.Domain.Enums;

namespace Yandex.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid UserId { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime? ProcessedAt { get; set; }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new Exception("Booking is already cancelled");
        Status = BookingStatus.Cancelled;
    }
}