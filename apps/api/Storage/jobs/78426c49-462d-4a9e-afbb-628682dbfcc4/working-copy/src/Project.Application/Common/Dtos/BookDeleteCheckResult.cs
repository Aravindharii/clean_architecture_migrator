using Project.Application.Validators;
namespace Project.Application.Common.Dtos;

public class BookDeleteCheckResult
{
    public bool CanDelete { get; set; }

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }

    public int LoanedCopies { get; set; }

    public string? Reason { get; set; }
}
