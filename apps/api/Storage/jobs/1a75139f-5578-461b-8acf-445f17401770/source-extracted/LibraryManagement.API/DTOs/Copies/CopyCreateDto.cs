namespace LibraryManagement.API.DTOs.Copies;

/// <summary>
/// Represents the data required to create a physical book copy.
/// </summary>
public class CopyCreateDto
{
    /// <summary>
    /// Gets or sets the physical location of the copy.
    /// </summary>
    public string Location { get; set; } = string.Empty;

}

