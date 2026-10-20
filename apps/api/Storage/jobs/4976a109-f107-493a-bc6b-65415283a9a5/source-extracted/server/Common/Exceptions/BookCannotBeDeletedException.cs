namespace LibraryManagement.Api.Common.Exceptions;

public class BookCannotBeDeletedException : AppException
{
    public int BookId { get; }
    public int TotalCopies { get; }
    public int AvailableCopies { get; }
    public int LoanedCopies { get; }
    public string Reason { get; }

    public BookCannotBeDeletedException(
        int bookId,
        int totalCopies,
        int availableCopies,
        int loanedCopies,
        string reason)
        : base("Book cannot be deleted.")
    {
        BookId = bookId;
        TotalCopies = totalCopies;
        AvailableCopies = availableCopies;
        LoanedCopies = loanedCopies;
        Reason = reason;
    }
}
