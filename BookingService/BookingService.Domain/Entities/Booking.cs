using BookingService.Domain.Enums;
using Shared.Domain.Entities;

namespace BookingService.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid EventId { get; set; }

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
        {
            return;
        }
        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}