using LibraryManagement.Api.Models.Common;

namespace LibraryManagement.Api.Models;

public class Book : ISoftDelete
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public int AuthorId { get; set; }

    public Author Author { get; set; } = null!;

    public string ISBN { get; set; } = string.Empty;

    public string? ISBN13 { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Language { get; set; } = "English";

    public int PublishedYear { get; set; }

    public string? Edition { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? CoverImage { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public ICollection<Copy> Copies { get; set; } = new List<Copy>();
}
