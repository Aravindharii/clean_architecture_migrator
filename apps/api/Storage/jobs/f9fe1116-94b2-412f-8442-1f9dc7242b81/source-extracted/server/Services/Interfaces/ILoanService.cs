using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services.Interfaces;

public interface ILoanService
{
    Task<IEnumerable<LoanDto>> GetAllAsync();
    Task<IEnumerable<LoanDto>> GetLoansForUserAsync(int userId);
    Task<LoanDto> GetByIdAsync(int id);
    Task<LoanDto> CreateAsync(LoanCreateValidator dto);
    Task UpdateAsync(int id, LoanUpdateValidator dto);
    Task ReturnLoanAsync(int id);
}
