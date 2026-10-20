namespace LibraryManagement.API.DTOs.Authors;

/// <summary>
/// Represents the data required to create an author.
/// </summary>
public class AuthorCreateDto
{
    /// <summary>
    /// Gets or sets the author's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional biography information.
    /// </summary>
    public string? Biography { get; set; }
}