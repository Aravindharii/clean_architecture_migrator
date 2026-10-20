using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Repositories.Interfaces;

public interface ICopyRepository
{
    Task<IEnumerable<Copy>> GetAllAsync();

    Task<Copy?> GetByIdAsync(int id);

    Task<Copy> AddAsync(Copy copy);

    Task UpdateAsync(Copy copy);

    Task<bool> HasActiveLoansAsync(int copyId);

    Task<bool> ExistsByBookIdAsync(int bookId);

    Task<int> GetCountAsync();

    Task<int> GetAvailableCountAsync();

    Task<int> GetIssuedCountAsync();

    Task<int> GetDamagedCountAsync();

    Task<IEnumerable<Copy>> QueryAsync(int? bookId = null, string? status = null);

    Task<(int TotalCopies, int LoanedCopies)> GetDeleteCheckCountsAsync(int bookId);

    Task<IEnumerable<Copy>> GetAllDeletedAsync();

    Task<Copy?> GetByIdIncludingDeletedAsync(int id);

    Task SoftDeleteAsync(Copy copy, string? deletedBy = null);

    Task RestoreAsync(Copy copy);

    Task HardDeleteAsync(Copy copy);

    Task<IEnumerable<Copy>> GetAllByBookIdIncludingDeletedAsync(int bookId);
}
