namespace Project.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task<IEnumerable<object>> GetAllAsync();
    Task<object?> GetByIdAsync(Guid id);
    Task AddAsync(object entity);
    Task UpdateAsync(object entity);
    Task DeleteAsync(Guid id);
}
