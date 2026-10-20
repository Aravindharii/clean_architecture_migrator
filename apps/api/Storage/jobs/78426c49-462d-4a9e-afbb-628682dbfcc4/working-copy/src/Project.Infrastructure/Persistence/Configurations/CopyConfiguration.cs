using Project.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Project.Infrastructure.Persistence.Configurations;

public class CopyConfiguration : IEntityTypeConfiguration<Copy>
{
    public void Configure(EntityTypeBuilder<Copy> builder)
    {
        builder.ToTable("Copies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Barcode)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("text");

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("text")
            .HasDefaultValue("Available");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        builder.Property(c => c.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(c => c.DeletedBy)
            .HasMaxLength(200)
            .HasColumnType("text");

        builder.HasOne(c => c.Book)
            .WithMany(b => b.Copies)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(c => c.BookId);
        builder.HasIndex(c => c.Barcode);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.IsDeleted);
    }
}
