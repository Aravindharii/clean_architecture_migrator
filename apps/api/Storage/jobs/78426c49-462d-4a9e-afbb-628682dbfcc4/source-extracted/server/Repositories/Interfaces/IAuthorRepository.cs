using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Repositories.Interfaces;

public interface IAuthorRepository
{
    Task<IEnumerable<Author>> GetAllAsync();

    Task<Author?> GetByIdAsync(int id);

    Task<Author> AddAsync(Author author);

    Task UpdateAsync(Author author);

    Task<bool> HasBooksAsync(int authorId);

    Task<int> GetCountAsync();

    Task<IEnumerable<Author>> GetAllDeletedAsync();

    Task<Author?> GetByIdIncludingDeletedAsync(int id);

    Task SoftDeleteAsync(Author author, string? deletedBy = null);

    Task RestoreAsync(Author author);

    Task HardDeleteAsync(Author author);
}
