using LibraryManagement.Api.Common;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Repositories;

public class UserRepository : SoftDeleteRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "adding", "User", _logger);
        }

        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating User {Id}", user.Id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "updating", "User", _logger);
        }
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .OrderBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<int> CountAdminsAsync()
    {
        return await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
    }
}
