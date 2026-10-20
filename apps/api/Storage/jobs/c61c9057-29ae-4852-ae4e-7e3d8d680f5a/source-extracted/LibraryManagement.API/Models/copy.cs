namespace LibraryManagement.API.Models;

/// <summary>
/// Represents a physical copy of a book in the library.
/// </summary>
public class Copy
{
    /// <summary>
    /// Gets or sets the unique identifier of the copy.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the associated book.
    /// </summary>
    public int BookId { get; set; }

    /// <summary>
    /// Gets or sets the unique barcode of the physical copy.
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the physical location of the copy.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the copy.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version used for optimistic concurrency.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the copy has been soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the associated book.
    /// </summary>
    public Book Book { get; set; } = null!;
}