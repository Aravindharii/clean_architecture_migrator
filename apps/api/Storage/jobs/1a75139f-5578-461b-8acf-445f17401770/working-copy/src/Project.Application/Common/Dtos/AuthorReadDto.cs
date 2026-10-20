namespace Project.Application.Common.Dtos;

/// <summary>
/// Represents author information returned by the API.
/// </summary>
public class AuthorReadDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the author.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the author's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional biography information.
    /// </summary>
    public string? Biography { get; set; }

    /// <summary>
    /// Gets or sets the author's version for optimistic concurrency.
    /// </summary>
    public int Version { get; set; }
}