using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services.Interfaces;

public interface IAuthorService
{
    Task<IEnumerable<AuthorDto>> GetAllAsync();
    Task<AuthorDto> GetByIdAsync(int id);
    Task<AuthorDto> CreateAsync(AuthorCreateValidator dto);
    Task UpdateAsync(int id, AuthorUpdateValidator dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<AuthorDto>> GetDeletedAsync();
    Task RestoreAsync(int id);
    Task HardDeleteAsync(int id);
}
