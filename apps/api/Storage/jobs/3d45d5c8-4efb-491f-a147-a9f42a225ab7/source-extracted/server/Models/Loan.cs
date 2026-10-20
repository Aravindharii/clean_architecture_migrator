namespace LibraryManagement.Api.Models;

public class Loan
{
    public int Id { get; set; }

    public int CopyId { get; set; }

    public Copy? Copy { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public int? UserId { get; set; }

    public User? User { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
