using Project.Application.Validators;
namespace Project.Application.Common.Dtos;

public class BookDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public int AuthorId { get; set; }

    public string Author { get; set; } = string.Empty;

    public string ISBN { get; set; } = string.Empty;

    public string? ISBN13 { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Language { get; set; } = "English";

    public int PublishedYear { get; set; }

    public string? Edition { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? CoverImage { get; set; }

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public List<CopyDto> Copies { get; set; } = new();
}
