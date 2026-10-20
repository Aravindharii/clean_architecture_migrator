using Project.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Project.Infrastructure.Persistence.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd();

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnType("text");

        builder.Property(b => b.Subtitle)
            .HasMaxLength(500)
            .HasColumnType("text");

        builder.Property(b => b.ISBN)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnType("text");

        builder.Property(b => b.ISBN13)
            .HasMaxLength(20)
            .HasColumnType("text");

        builder.Property(b => b.Category)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("text");

        builder.Property(b => b.Language)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("text")
            .HasDefaultValue("English");

        builder.Property(b => b.PublishedYear)
            .IsRequired()
            .HasColumnType("integer");

        builder.Property(b => b.Edition)
            .HasMaxLength(50)
            .HasColumnType("text");

        builder.Property(b => b.Description)
            .IsRequired()
            .HasMaxLength(2000)
            .HasColumnType("text");

        builder.Property(b => b.CoverImage)
            .HasColumnType("text");

        builder.Property(b => b.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(b => b.IsDeleted)
            .IsRequired()
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        builder.Property(b => b.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(b => b.DeletedBy)
            .HasMaxLength(200)
            .HasColumnType("text");

        builder.HasOne(b => b.Author)
            .WithMany(a => a.Books)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(b => b.AuthorId);
        builder.HasIndex(b => b.ISBN);
        builder.HasIndex(b => b.Category);
        builder.HasIndex(b => b.IsDeleted);
    }
}
