using Project.Application.Validators;
using Project.Application.Common.Interfaces;
using Project.Application.Common.Dtos;

namespace Project.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly ICopyRepository _copyRepository;
    private readonly ILoanRepository _loanRepository;

    public DashboardService(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        ICopyRepository copyRepository,
        ILoanRepository loanRepository)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _copyRepository = copyRepository;
        _loanRepository = loanRepository;
    }

    public async Task<DashboardDto> GetStatsAsync()
    {
        var totalBooks = await _bookRepository.GetCountAsync();
        var totalAuthors = await _authorRepository.GetCountAsync();
        var totalCopies = await _copyRepository.GetCountAsync();
        var availableCopies = await _copyRepository.GetAvailableCountAsync();
        var issuedCopies = await _copyRepository.GetIssuedCountAsync();
        var damagedCopies = await _copyRepository.GetDamagedCountAsync();
        var activeLoans = await _loanRepository.GetActiveLoanCountAsync();
        var overdueLoans = await _loanRepository.GetOverdueLoanCountAsync();

        return new DashboardDto
        {
            TotalBooks = totalBooks,
            TotalAuthors = totalAuthors,
            TotalCopies = totalCopies,
            AvailableCopies = availableCopies,
            IssuedCopies = issuedCopies,
            DamagedCopies = damagedCopies,
            ActiveLoans = activeLoans,
            OverdueLoans = overdueLoans
        };
    }
}
