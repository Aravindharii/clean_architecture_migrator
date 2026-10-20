namespace Project.Application.Common.Dtos;

/// <summary>
/// Represents the data used to update an existing author.
/// </summary>
public class AuthorUpdateDto
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