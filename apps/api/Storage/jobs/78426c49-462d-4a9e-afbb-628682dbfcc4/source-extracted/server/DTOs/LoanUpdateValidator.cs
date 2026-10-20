namespace LibraryManagement.Api.DTOs;

public class LoanUpdateValidator
{
    public int CopyId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    public DateTime DueDate { get; set; }

    public string Status { get; set; } = string.Empty;
}
