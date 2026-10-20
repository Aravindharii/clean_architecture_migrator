using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Repositories;

/// <summary>
/// Provides database operations for users.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="context">Application database context.</param>
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }


    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(user => user.Email == email);
    }


    /// <summary>
    /// Checks whether an email already exists.
    /// </summary>
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users
            .AnyAsync(user => user.Email == email);
    }


    /// <summary>
    /// Adds a new user.
    /// </summary>
    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);

        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
{
    _context.Users.Update(user);

    await _context.SaveChangesAsync();
}

/// <summary>
/// Retrieves a user by refresh token.
/// </summary>
public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
{
    return await _context.Users
        .FirstOrDefaultAsync(
            user => user.RefreshToken == refreshToken);
}

public async Task<User?> GetByIdAsync(int id)
{
    return await _context.Users
        .FirstOrDefaultAsync(user => user.Id == id);
}

}

