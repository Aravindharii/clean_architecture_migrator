using Project.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Project.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("text");

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("text");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnType("text");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnType("text");

        builder.Property(u => u.Role)
            .IsRequired()
            .HasColumnType("integer")
            .HasDefaultValue(UserRole.Member);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        builder.Property(u => u.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.DeletedBy)
            .HasMaxLength(200)
            .HasColumnType("text");

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500)
            .HasColumnType("text");

        builder.Property(u => u.RefreshTokenExpiryTime)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.Role);

        builder.HasIndex(u => u.IsDeleted);
    }
}
