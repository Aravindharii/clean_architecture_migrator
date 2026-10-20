namespace LibraryManagement.API.DTOs.Books;
/// <summary>
/// Represents the data required to create a new book
/// in the library catalogue.
/// </summary>
public class BookCreateDto
{
    /// <summary>
    /// Gets or sets the title of the book.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional subtitle of the book.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets the ISBN identifier of the book edition.
    /// </summary>
    public string ISBN { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional 13-digit ISBN.
    /// </summary>
    public string? ISBN13 { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the book category.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the identifiers of the book authors.
    /// </summary>
    public List<int> AuthorIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the language of the book.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the publication year.
    /// </summary>
    public int PublishedYear { get; set; }

    /// <summary>
    /// Gets or sets the edition information.
    /// </summary>
    public string? Edition { get; set; }

    /// <summary>
    /// Gets or sets the description or summary of the book.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cover image reference.
    /// </summary>
    public string? CoverImage { get; set; }

    /// <summary>
    /// Gets or sets the total number of copies owned by the library.
    /// </summary>
    public int TotalCopies { get; set; }

   
}