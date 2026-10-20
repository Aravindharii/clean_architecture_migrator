using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Common.Interfaces;

public interface ICopyService
{
    Task<IEnumerable<CopyDto>> GetAllAsync();
    Task<CopyDto> GetByIdAsync(int id);
    Task<CopyDto> CreateAsync(CopyCreateValidator dto);
    Task UpdateAsync(int id, CopyUpdateValidator dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<CopyDto>> QueryAsync(int? bookId = null, string? status = null);
    Task<IEnumerable<CopyDto>> GetDeletedAsync();
    Task RestoreAsync(int id);
    Task HardDeleteAsync(int id);
}
