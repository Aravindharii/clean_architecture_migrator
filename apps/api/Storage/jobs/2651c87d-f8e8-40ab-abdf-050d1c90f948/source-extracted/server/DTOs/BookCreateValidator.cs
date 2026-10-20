namespace LibraryManagement.Api.DTOs;

public class BookCreateValidator
{
    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public int AuthorId { get; set; }

    public string ISBN { get; set; } = string.Empty;

    public string? ISBN13 { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Language { get; set; } = "English";

    public int PublishedYear { get; set; }

    public string? Edition { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? CoverImage { get; set; }

    public int TotalCopies { get; set; } = 0;
}
