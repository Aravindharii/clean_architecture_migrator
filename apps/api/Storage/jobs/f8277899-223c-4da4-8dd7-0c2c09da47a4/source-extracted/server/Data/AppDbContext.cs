using LibraryManagement.Api.Models;
using LibraryManagement.Api.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Copy> Copies => Set<Copy>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<User> Users => Set<User>();

    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, [modelBuilder]);
            }
        }
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : class, ISoftDelete
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override int SaveChanges()
    {
        UpdateSoftDeleteTimestamps();
        TouchUpdatedTimestamps();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateSoftDeleteTimestamps();
        TouchUpdatedTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateSoftDeleteTimestamps();
        TouchUpdatedTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateSoftDeleteTimestamps();
        TouchUpdatedTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateSoftDeleteTimestamps()
    {
        var entries = ChangeTracker.Entries<ISoftDelete>()
            .Where(e => e.State == EntityState.Modified && e.Property(p => p.IsDeleted).CurrentValue);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified && entry.Property(p => p.IsDeleted).IsModified)
            {
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }
    }

    private void TouchUpdatedTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            if (entry.Metadata.FindProperty("UpdatedAt")?.ClrType == typeof(DateTime))
            {
                entry.CurrentValues["UpdatedAt"] = DateTime.UtcNow;
            }
        }
    }
}
