namespace Project.Application.Common.Dtos;

/// <summary>
/// Represents the data returned for a physical book copy.
/// </summary>
public class CopyReadDto
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
/// Gets or sets the title of the associated book.
/// </summary>
public string BookTitle { get; set; } = string.Empty;

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
    /// Gets or sets the version used for optimistic concurrency control.
    /// </summary>
    public int Version { get; set; }
}