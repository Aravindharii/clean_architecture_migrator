namespace Project.Domain.Entities;

/// <summary>
/// Represents a category used to classify books.
/// </summary>
public class Category
{
    /// <summary>
    /// Gets or sets the unique identifier of the category.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the books belonging to this category.
    /// </summary>
    public ICollection<Book> Books { get; set; }
        = new List<Book>();
}