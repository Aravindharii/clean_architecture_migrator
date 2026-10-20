namespace LibraryManagement.API.Models;

/// <summary>
/// Represents the relationship between a book and an author.
/// </summary>
public class BookAuthor
{
    /// <summary>
    /// Gets or sets the associated book identifier.
    /// </summary>
    public int BookId { get; set; }

    /// <summary>
    /// Gets or sets the associated author identifier.
    /// </summary>
    public int AuthorId { get; set; }

    /// <summary>
    /// Gets or sets the associated book.
    /// </summary>
    public Book Book { get; set; } = null!;

    /// <summary>
    /// Gets or sets the associated author.
    /// </summary>
    public Author Author { get; set; } = null!;
}