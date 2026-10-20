using Project.Domain.Entities;

namespace Project.Domain.Entities;

public class Author : ISoftDelete
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
