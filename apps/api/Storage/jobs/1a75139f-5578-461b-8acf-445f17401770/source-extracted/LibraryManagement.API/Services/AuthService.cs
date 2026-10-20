using FluentValidation;
using LibraryManagement.API.DTOs.Auth;
using LibraryManagement.API.Exceptions;
using LibraryManagement.API.Models;
using LibraryManagement.API.Repositories;
using Microsoft.AspNetCore.Identity;
using LibraryManagement.API.Enums;

namespace LibraryManagement.API.Services;

/// <summary>
/// Provides authentication related business operations.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtService _jwtService;


    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    public AuthService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    IJwtService jwtService)
{
    _userRepository = userRepository;
    _passwordHasher = passwordHasher;
    _jwtService = jwtService;
}


    /// <summary>
    /// Registers a new user.
    /// </summary>
    public async Task RegisterAsync(RegisterDto registerDto)
    {
        var email = registerDto.Email
            .Trim()
            .ToLower();


        if (await _userRepository.ExistsByEmailAsync(email))
        {
            throw new DuplicateResourceException(
                "A user with this email already exists.");
        }


        var user = new User
        {
            FirstName = registerDto.FirstName.Trim(),

            LastName = registerDto.LastName.Trim(),

            Email = email,

            Role = UserRole.User,

            IsActive = true,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow
        };


        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                registerDto.Password);


        await _userRepository.AddAsync(user);
    }

/// <summary>
/// Authenticates a user.
/// </summary>
public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
{
    var email = loginDto.Email
        .Trim()
        .ToLower();


    var user = await _userRepository.GetByEmailAsync(email);


    if (user == null)
    {
        throw new UnauthorizedAccessException(
            "Invalid email or password.");
    }


    if (!user.IsActive)
    {
        throw new UnauthorizedAccessException(
            "User account is inactive.");
    }


    var passwordResult =
        _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            loginDto.Password);


    if (passwordResult == PasswordVerificationResult.Failed)
    {
        throw new UnauthorizedAccessException(
            "Invalid email or password.");
    }


    var accessToken =
    _jwtService.GenerateAccessToken(user);


var refreshToken =
    _jwtService.GenerateRefreshToken();


user.RefreshToken = refreshToken;

user.RefreshTokenExpiry =
    DateTime.UtcNow.AddDays(7);


await _userRepository.UpdateAsync(user);



    return new AuthResponseDto
    {
        Message = "Login successful.",
        AccessToken = accessToken,
        RefreshToken = refreshToken
    };
}
/// <summary>
/// Generates new authentication tokens using a valid refresh token.
/// </summary>
/// <param name="refreshTokenDto">
/// Refresh token used to obtain new authentication tokens.
/// </param>
/// <returns>
/// Authentication response containing new access and refresh tokens.
/// </returns>
public async Task<AuthResponseDto> RefreshTokenAsync(
    RefreshTokenDto refreshTokenDto)
{
    var user = await _userRepository
        .GetByRefreshTokenAsync(refreshTokenDto.RefreshToken);

    if (user == null)
    {
        throw new UnauthorizedAccessException(
            "Invalid refresh token.");
    }

    if (!user.IsActive)
    {
        throw new UnauthorizedAccessException(
            "User account is inactive.");
    }

    if (user.RefreshTokenExpiry == null ||
        user.RefreshTokenExpiry <= DateTime.UtcNow)
    {
        throw new UnauthorizedAccessException(
            "Refresh token has expired.");
    }

    var accessToken =
        _jwtService.GenerateAccessToken(user);

    var newRefreshToken =
        _jwtService.GenerateRefreshToken();

    user.RefreshToken = newRefreshToken;

    user.RefreshTokenExpiry =
        DateTime.UtcNow.AddDays(7);

    await _userRepository.UpdateAsync(user);

    return new AuthResponseDto
    {
        Message = "Token refreshed successfully.",
        AccessToken = accessToken,
        RefreshToken = newRefreshToken
    };
}

/// <summary>
/// Logs out a user by invalidating the refresh token.
/// </summary>
/// <param name="userId">
/// Identifier of the authenticated user.
/// </param>
public async Task LogoutAsync(int userId)
{
    var user = await _userRepository.GetByIdAsync(userId);

    if (user == null)
    {
        throw new UnauthorizedAccessException(
            "User not found.");
    }

    user.RefreshToken = null;
    user.RefreshTokenExpiry = null;
    user.UpdatedAt = DateTime.UtcNow;

    await _userRepository.UpdateAsync(user);
}
}