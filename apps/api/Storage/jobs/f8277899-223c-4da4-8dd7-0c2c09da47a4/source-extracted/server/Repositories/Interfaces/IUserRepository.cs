using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByIdAsync(int id);

    Task<User?> GetByRefreshTokenAsync(string refreshToken);

    Task<User> AddAsync(User user);

    Task UpdateAsync(User user);

    Task<bool> ExistsByEmailAsync(string email);

    Task<IEnumerable<User>> GetAllAsync();

    Task<int> CountAdminsAsync();

    Task<IEnumerable<User>> GetAllDeletedAsync();

    Task<User?> GetByIdIncludingDeletedAsync(int id);

    Task SoftDeleteAsync(User user, string? deletedBy = null);

    Task RestoreAsync(User user);

    Task HardDeleteAsync(User user);
}
