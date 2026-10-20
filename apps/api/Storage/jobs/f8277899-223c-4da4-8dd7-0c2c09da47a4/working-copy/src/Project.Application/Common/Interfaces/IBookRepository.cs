using Project.Domain.Entities;

namespace Project.Application.Common.Interfaces;

public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();

    Task<(IEnumerable<Book> Books, int TotalCount)> GetAllAsync(string? q, string? category, bool? available, int page, int size, string? sort);

    Task<Book?> GetByIdAsync(int id);

    Task<Book> AddAsync(Book book);

    Task UpdateAsync(Book book);

    Task<int> GetCountAsync();

    Task<IEnumerable<Book>> GetAllDeletedAsync();

    Task<Book?> GetByIdIncludingDeletedAsync(int id);

    Task SoftDeleteAsync(Book book, string? deletedBy = null);

    Task RestoreAsync(Book book);

    Task HardDeleteAsync(Book book);
}
