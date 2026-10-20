using LibraryManagement.API.Models;

namespace LibraryManagement.API.Services;

/// <summary>
/// Defines operations for JWT token generation.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates an access token for a user.
    /// </summary>
    /// <param name="user">
    /// User details used to create JWT claims.
    /// </param>
    /// <returns>
    /// Generated JWT access token.
    /// </returns>
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}