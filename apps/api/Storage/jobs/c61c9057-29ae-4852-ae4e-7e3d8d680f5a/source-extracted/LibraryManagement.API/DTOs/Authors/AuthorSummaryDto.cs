namespace LibraryManagement.API.DTOs.Authors;

/// <summary>
/// Represents a simplified author object
/// used inside other resources.
/// </summary>
public class AuthorSummaryDto
{
    /// <summary>
    /// Gets or sets the author identifier.
    /// </summary>
    public int Id { get; set; }


    /// <summary>
    /// Gets or sets the author name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}