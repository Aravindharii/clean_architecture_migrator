using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Api.Data.Configurations;

public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("text");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        builder.Property(a => a.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.DeletedBy)
            .HasMaxLength(200)
            .HasColumnType("text");

        builder.HasIndex(a => a.Name);
        builder.HasIndex(a => a.IsDeleted);
    }
}
