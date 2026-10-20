namespace LibraryManagement.Api.DTOs;

public class CopyDto
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
