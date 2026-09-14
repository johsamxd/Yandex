using System.ComponentModel.DataAnnotations;
using Yandex.Domain.Enums;

namespace Yandex.Domain.Entities;

public class Booking : BaseEntity
{
    [Required]
    public required Guid EventId { get; set; }

    [Required]
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
}