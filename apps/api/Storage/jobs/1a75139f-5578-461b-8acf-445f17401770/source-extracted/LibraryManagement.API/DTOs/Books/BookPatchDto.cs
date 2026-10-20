namespace LibraryManagement.API.DTOs.Books;

/// <summary>
/// Represents the data allowed when partially updating
/// an existing book.
/// </summary>
public class BookPatchDto
{
    /// <summary>
    /// Gets or sets the optional title of the book.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the optional subtitle.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets the optional identifier of the book category.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the optional identifiers of the book authors.
    /// When supplied, the existing author relationships are replaced.
    /// </summary>
    public List<int>? AuthorIds { get; set; }

    /// <summary>
    /// Gets or sets the optional language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the optional publication year.
    /// </summary>
    public int? PublishedYear { get; set; }

    /// <summary>
    /// Gets or sets the optional edition information.
    /// </summary>
    public string? Edition { get; set; }

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the optional cover image reference.
    /// </summary>
    public string? CoverImage { get; set; }


}