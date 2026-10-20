namespace LibraryManagement.Api.DTOs;

public class DashboardDto
{
    public int TotalBooks { get; set; }

    public int TotalAuthors { get; set; }

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }

    public int IssuedCopies { get; set; }

    public int DamagedCopies { get; set; }

    public int ActiveLoans { get; set; }

    public int OverdueLoans { get; set; }
}
