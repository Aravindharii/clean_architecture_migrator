using Project.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Represents the application's database context.
///
/// Entity Framework Core uses this context to map entities
/// to database tables and perform database operations.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">
    /// Configuration options provided by ASP.NET Core's Dependency Injection container.
    /// </param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Represents the Books table in the database.
    /// </summary>
    public DbSet<Book> Books => Set<Book>();

    /// <summary>
    /// Represents the Authors table in the database.
    /// </summary>
    public DbSet<Author> Authors => Set<Author>();

    /// <summary>
    /// Represents the Categories table in the database.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Represents the Inventories table in the database.
    /// </summary>
    public DbSet<Inventory> Inventories => Set<Inventory>();

    /// <summary>
    /// Represents the BookAuthors junction table in the database.
    /// </summary>
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();

    ///phase 2 
    /// /// <summary>
    /// Represents the Users table in the database.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
/// Represents the Copies table in the database.
/// </summary>
public DbSet<Copy> Copies => Set<Copy>();

    /// <summary>
    /// Configures entity relationships and database mappings.
    /// </summary>
    /// <param name="modelBuilder">
    /// Provides a simple API surface for configuring the EF Core model.
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

     // Configure User
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique();

    // Configure the BookAuthor junction table (many-to-many)
    modelBuilder.Entity<BookAuthor>()
        .HasKey(ba => new { ba.BookId, ba.AuthorId });

    modelBuilder.Entity<BookAuthor>()
        .HasOne(ba => ba.Book)
        .WithMany(b => b.BookAuthors)
        .HasForeignKey(ba => ba.BookId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<BookAuthor>()
        .HasOne(ba => ba.Author)
        .WithMany(a => a.BookAuthors)
        .HasForeignKey(ba => ba.AuthorId)
        .OnDelete(DeleteBehavior.Restrict);


    // Configure Book-Category relationship (one-to-many)
    modelBuilder.Entity<Book>()
        .HasOne(b => b.Category)
        .WithMany(c => c.Books)
        .HasForeignKey(b => b.CategoryId)
        .OnDelete(DeleteBehavior.Restrict);


        // Configure Book-Inventory relationship (one-to-one)
    modelBuilder.Entity<Book>()
        .HasOne(b => b.Inventory)
        .WithOne(i => i.Book)
        .HasForeignKey<Inventory>(i => i.BookId)
        .OnDelete(DeleteBehavior.Cascade);

    //book -copy relation
    modelBuilder.Entity<Copy>()
    .HasOne(copy => copy.Book)
    .WithMany(book => book.Copies)
    .HasForeignKey(copy => copy.BookId)
    .OnDelete(DeleteBehavior.Restrict);

    // Configure Book optimistic concurrency
    modelBuilder.Entity<Book>()
    .Property(b => b.Version)
    .IsConcurrencyToken();

    // Configure Copy optimistic concurrency
    modelBuilder.Entity<Copy>()
    .Property(c => c.Version)
    .IsConcurrencyToken();

    // Configure Book soft delete
modelBuilder.Entity<Book>()
    .HasQueryFilter(book => !book.IsDeleted);

modelBuilder.Entity<BookAuthor>()
    .HasQueryFilter(ba => !ba.Book.IsDeleted);

modelBuilder.Entity<Inventory>()
    .HasQueryFilter(i => !i.Book.IsDeleted);

modelBuilder.Entity<Copy>()
    .HasQueryFilter(copy =>
        !copy.IsDeleted &&
        !copy.Book.IsDeleted);

     // Configure Author optimistic concurrency
    modelBuilder.Entity<Author>()
    .Property(a => a.Version)
    .IsConcurrencyToken();


    // Seed Categories
    modelBuilder.Entity<Category>()
        .HasData(
            new Category
            {
                Id = 1,
                Name = "Programming"
            },
            new Category
            {
                Id = 2,
                Name = "Database"
            },
            new Category
            {
                Id = 3,
                Name = "Artificial Intelligence"
            }
        );


    // Seed Authors
    modelBuilder.Entity<Author>()
        .HasData(
            new Author
            {
                Id = 1,
                Name = "Robert C. Martin",
                Biography = "Software engineering author"
            },
            new Author
            {
                Id = 2,
                Name = "Andrew Tanenbaum",
                Biography = "Computer science author"
            },
            new Author
            {
                Id = 3,
                Name = "Martin Fowler",
                Biography = "Software development author"
            }
        );
}}