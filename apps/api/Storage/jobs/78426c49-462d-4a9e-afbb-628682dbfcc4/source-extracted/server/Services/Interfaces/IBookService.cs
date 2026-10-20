using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services.Interfaces;

public interface IBookService
{
    Task<PagedResponse<BookDto>> GetAllAsync(string? q, string? category, bool? available, int page, int size, string? sort);
    Task<BookDto> GetByIdAsync(int id);
    Task<BookDto> CreateAsync(BookCreateValidator dto);
    Task UpdateAsync(int id, BookUpdateValidator dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<BookDto>> GetAllAsync();
    Task<BookDeleteCheckResult> CheckDeletionEligibilityAsync(int id);
    Task<IEnumerable<BookDto>> GetDeletedAsync();
    Task RestoreAsync(int id);
    Task HardDeleteAsync(int id);
}
