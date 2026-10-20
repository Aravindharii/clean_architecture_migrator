namespace Project.Domain.Entities;

/// <summary>
/// Represents a book in the library catalogue.
/// 
/// This entity is mapped to the <c>Books</c> table by Entity Framework Core
/// and stores the metadata required for catalogue operations.
/// </summary>
public class Book
{
    /// <summary>
    /// Gets or sets the unique identifier for the book.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the version used for optimistic concurrency control.
    /// </summary>
public int Version { get; set; }

    /// <summary>
    /// Gets or sets the main title of the book.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional subtitle of the book.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets the International Standard Book Number (ISBN).
    /// </summary>
    public string ISBN { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional 13-digit ISBN.
    /// </summary>
    public string? ISBN13 { get; set; }

    /// <summary>
    /// Gets or sets the language in which the book is written.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the publication year.
    /// </summary>
    public int PublishedYear { get; set; }

    /// <summary>
    /// Gets or sets the edition of the book, if available.
    /// </summary>
    public string? Edition { get; set; }

    /// <summary>
    /// Gets or sets a short description or summary of the book.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL or relative path of the book cover image.
    /// </summary>
    public string? CoverImage { get; set; }

    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category of this book.
    /// </summary>
    public Category Category { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the book has been soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the authors associated with this book.
    /// </summary>
    public ICollection<BookAuthor> BookAuthors { get; set; }
        = new List<BookAuthor>();

    /// <summary>
    /// Gets or sets the inventory information for this book.
    /// </summary>
    public Inventory Inventory { get; set; } = null!;

    /// <summary>
/// Gets or sets the physical copies of this book.
/// </summary>
public ICollection<Copy> Copies { get; set; }
    = new List<Copy>();
}