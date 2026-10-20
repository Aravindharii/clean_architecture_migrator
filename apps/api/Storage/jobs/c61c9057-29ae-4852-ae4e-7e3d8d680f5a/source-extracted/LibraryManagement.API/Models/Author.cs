namespace LibraryManagement.API.Models;

/// <summary>
/// Represents an author of one or more books.
/// </summary>
public class Author
{
    /// <summary>
    /// Gets or sets the unique identifier of the author.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the author's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional biography information about the author.
    /// </summary>
    public string? Biography { get; set; }

    /// <summary>
    /// Gets or sets the version used for optimistic concurrency.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the books associated with this author.
    /// </summary>
    public ICollection<BookAuthor> BookAuthors { get; set; }
        = new List<BookAuthor>();
}