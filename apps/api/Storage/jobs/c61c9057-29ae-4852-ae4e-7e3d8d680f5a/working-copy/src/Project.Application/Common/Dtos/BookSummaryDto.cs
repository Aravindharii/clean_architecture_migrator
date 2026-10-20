namespace Project.Application.Common.Dtos;

/// <summary>
/// Represents a short book representation
/// used for book lists.
/// </summary>
public class BookSummaryDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the book.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the book.
    /// </summary>
    public string Title { get; set; } = string.Empty;
}