using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public class ProcessedBookingConfiguration : IEntityTypeConfiguration<ProcessedBooking>
{
    public void Configure(EntityTypeBuilder<ProcessedBooking> builder)
    {
        builder.ToTable("ProcessedBookings");

        // Ключ — идентификатор брони из сервиса Bookings: вторая запись с тем же ключом невозможна
        builder.HasKey(x => x.BookingId);
        builder.Property(x => x.BookingId).ValueGeneratedNever();
        builder.Property(x => x.EventId).IsRequired();
        builder.Property(x => x.ProcessedAt).IsRequired();
    }
}
