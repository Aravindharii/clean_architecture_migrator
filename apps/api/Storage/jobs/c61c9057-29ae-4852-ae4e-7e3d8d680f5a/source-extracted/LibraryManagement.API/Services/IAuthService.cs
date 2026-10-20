using LibraryManagement.API.DTOs.Auth;

namespace LibraryManagement.API.Services;

/// <summary>
/// Defines authentication related operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="registerDto">Registration details.</param>
    Task RegisterAsync(RegisterDto registerDto);


    /// <summary>
    /// Authenticates a user.
    /// </summary>
    /// <param name="loginDto">Login credentials.</param>
    /// <returns>Authentication response containing tokens.</returns>
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

    /// <summary>
/// Generates new authentication tokens using a valid refresh token.
/// </summary>
/// <param name="refreshTokenDto">
/// Refresh token used to obtain new authentication tokens.
/// </param>
/// <returns>
/// Authentication response containing new access and refresh tokens.
/// </returns>
Task<AuthResponseDto> RefreshTokenAsync(
    RefreshTokenDto refreshTokenDto);

    Task LogoutAsync(int userId);
}