using Project.Domain.Entities;
using Project.Application.Services;
using Project.Application.Common.Interfaces;

namespace Project.Application.Authentication;

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