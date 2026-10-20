using LibraryManagement.API.Models;

namespace LibraryManagement.API.Repositories;

/// <summary>
/// Defines database operations for users.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    /// <param name="email">User email address.</param>
    /// <returns>The matching user or null.</returns>
    Task<User?> GetByEmailAsync(string email);


    /// <summary>
    /// Checks whether a user exists with the given email.
    /// </summary>
    /// <param name="email">User email address.</param>
    /// <returns>True if email exists, otherwise false.</returns>
    Task<bool> ExistsByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);


    /// <summary>
    /// Adds a new user to the database.
    /// </summary>
    /// <param name="user">User entity to add.</param>
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    /// <summary>
/// Retrieves a user by refresh token.
/// </summary>
/// <param name="refreshToken">
/// Refresh token associated with the user.
/// </param>
/// <returns>
/// The user associated with the refresh token, if found.
/// </returns>
Task<User?> GetByRefreshTokenAsync(string refreshToken);
}