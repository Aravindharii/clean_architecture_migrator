namespace LibraryManagement.Api.DTOs;

public class LoanDto
{
    public int Id { get; set; }

    public int CopyId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string BookTitle { get; set; } = string.Empty;

    public string BorrowerName { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
