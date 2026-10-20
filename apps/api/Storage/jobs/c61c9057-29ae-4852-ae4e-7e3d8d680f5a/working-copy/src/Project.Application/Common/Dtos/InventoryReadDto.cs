namespace Project.Application.Common.Dtos;

/// <summary>
/// Represents the availability information of a book.
/// </summary>
public class InventoryReadDto
{
    /// <summary>
    /// Gets or sets the book identifier.
    /// </summary>
    public int BookId { get; set; }

    /// <summary>
    /// Gets or sets the book title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISBN of the book.
    /// </summary>
    public string ISBN { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of copies.
    /// </summary>
    public int TotalCopies { get; set; }

    /// <summary>
    /// Gets or sets the number of available copies.
    /// </summary>
    public int AvailableCopies { get; set; }

    /// <summary>
    /// Gets or sets the number of copies currently on loan.
    /// </summary>
    public int LoanedCopies { get; set; }
}