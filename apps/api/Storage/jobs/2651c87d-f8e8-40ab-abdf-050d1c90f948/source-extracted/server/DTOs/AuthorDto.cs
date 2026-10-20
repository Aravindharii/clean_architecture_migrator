namespace LibraryManagement.Api.DTOs;

public class AuthorDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public int BookCount { get; set; }

    public List<BookDto>? Books { get; set; }
}