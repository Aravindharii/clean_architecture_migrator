namespace LibraryManagement.API.Models;

/// <summary>
/// Represents the availability information of a book.
/// </summary>
public class Inventory
{
    /// <summary>
    /// Gets or sets the unique identifier of the inventory record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the associated book identifier.
    /// </summary>
    public int BookId { get; set; }

    /// <summary>
    /// Gets or sets the total number of copies.
    /// </summary>
    public int TotalCopies { get; set; }

    /// <summary>
    /// Gets or sets the number of currently available copies.
    /// </summary>
    public int AvailableCopies { get; set; }

    /// <summary>
    /// Gets or sets the associated book.
    /// </summary>
    public Book Book { get; set; } = null!;
}