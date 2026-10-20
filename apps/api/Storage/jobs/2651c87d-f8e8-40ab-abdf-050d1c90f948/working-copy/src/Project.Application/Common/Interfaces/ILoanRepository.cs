using Project.Domain.Entities;

namespace Project.Application.Common.Interfaces;

public interface ILoanRepository
{
    Task<IEnumerable<Loan>> GetAllAsync();

    Task<IEnumerable<Loan>> GetByUserIdAsync(int userId);

    Task<Loan?> GetByIdAsync(int id);

    Task<Loan> AddAsync(Loan loan);

    Task UpdateAsync(Loan loan);

    Task DeleteAsync(Loan loan);

    Task<bool> HasActiveLoanForCopyAsync(int copyId);

    Task<int> GetActiveLoanCountAsync();

    Task<int> GetOverdueLoanCountAsync();
}
