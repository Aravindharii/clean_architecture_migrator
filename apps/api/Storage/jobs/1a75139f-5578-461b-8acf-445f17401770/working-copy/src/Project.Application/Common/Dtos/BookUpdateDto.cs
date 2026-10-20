namespace Project.Application.Common.Dtos;
/// <summary>
/// Represents the data allowed when updating
/// an existing book.
/// </summary>
public class BookUpdateDto
{
    /// <summary>
    /// Gets or sets the title of the book.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional subtitle.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the book category.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the identifiers of the book authors.
    /// </summary>
    public List<int> AuthorIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the language.
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
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cover image reference.
    /// </summary>
    public string? CoverImage { get; set; }

   
}