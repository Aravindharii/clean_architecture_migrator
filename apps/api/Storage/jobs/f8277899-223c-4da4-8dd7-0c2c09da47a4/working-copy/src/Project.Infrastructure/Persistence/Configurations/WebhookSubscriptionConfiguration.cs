using Project.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Project.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .ValueGeneratedOnAdd();

        builder.Property(w => w.CallbackUrl)
            .IsRequired()
            .HasMaxLength(2000)
            .HasColumnType("text");

        builder.Property(w => w.Secret)
            .IsRequired()
            .HasMaxLength(512)
            .HasColumnType("text");

        builder.Property(w => w.EventTypes)
            .IsRequired()
            .HasColumnType("text[]");

        builder.Property(w => w.IsActive)
            .IsRequired()
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        builder.Property(w => w.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(w => w.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(w => w.IsActive);
        builder.HasIndex(w => w.CreatedAt);
    }
}
