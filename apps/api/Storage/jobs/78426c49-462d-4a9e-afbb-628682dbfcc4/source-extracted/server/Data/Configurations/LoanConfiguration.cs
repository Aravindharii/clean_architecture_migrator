using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Api.Data.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedOnAdd();

        builder.Property(l => l.BorrowerName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("text");

        builder.Property(l => l.IssueDate)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(l => l.DueDate)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(l => l.ReturnDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(l => l.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("text")
            .HasDefaultValue("Active");

        builder.Property(l => l.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasOne(l => l.Copy)
            .WithMany(c => c.Loans)
            .HasForeignKey(l => l.CopyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(l => l.CopyId);
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.DueDate);
    }
}
