using Project.Application.Common.Dtos;
namespace Project.Application.Validators;

public class LoanCreateValidator
{
    public int CopyId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(14);
}
