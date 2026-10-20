using Project.Application.Common.Dtos;
namespace Project.Application.Common.Dtos;

using Project.Application.Common.Dtos;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the book information returned by the API.
/// </summary>
public class BookReadDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the book.
    /// </summary>
    public int Id { get; set; }

     /// <summary>
    /// Gets or sets the version used for optimistic concurrency control.
    /// </summary>
    public int Version { get; set; }


    /// <summary>
    /// Gets or sets the title of the book.
    /// </summary>
    public string Title { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the optional subtitle.
    /// </summary>
    public string? Subtitle { get; set; }


    /// <summary>
    /// Gets or sets the authors of the book.
    /// </summary>
    public List<AuthorSummaryDto> Authors { get; set; } = [];


    /// <summary>
    /// Gets or sets the ISBN identifier.
    /// </summary>
    public string ISBN { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the optional 13-digit ISBN.
    /// </summary>
    [JsonPropertyName("isbn13")]
    public string? ISBN13 { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the book category.
    /// </summary>
    public int CategoryId { get; set; }
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Category { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string Language { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the publication year.
    /// </summary>
    public int PublishedYear { get; set; }


    /// <summary>
    /// Gets or sets the edition.
    /// </summary>
    public string? Edition { get; set; }


    /// <summary>
    /// Gets or sets the book description.
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


    /// <summary>
    /// Gets or sets the number of copies currently available.
    /// </summary>
    public int AvailableCopies { get; set; }


    /// <summary>
    /// Gets or sets the number of copies currently on loan.
    /// </summary>
    public int LoanedCopies { get; set; }
}