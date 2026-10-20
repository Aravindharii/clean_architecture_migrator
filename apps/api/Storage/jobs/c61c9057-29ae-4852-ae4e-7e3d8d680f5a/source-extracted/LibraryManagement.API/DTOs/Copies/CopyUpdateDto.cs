namespace LibraryManagement.API.DTOs.Copies;

/// <summary>
/// Represents the data required to update a physical book copy.
/// </summary>
public class CopyUpdateDto
{
    /// <summary>
    /// Gets or sets the physical location of the copy.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the copy.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}