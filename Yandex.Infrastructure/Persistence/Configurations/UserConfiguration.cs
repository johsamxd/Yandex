using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yandex.Domain.Entities;

namespace Yandex.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(u => u.Login).IsRequired().HasMaxLength(100);
        builder.HasIndex(u => u.Login).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(64);
    }
}